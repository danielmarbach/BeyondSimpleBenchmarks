# Beyond simple benchmarks—A practical guide to optimizing code with BenchmarkDotNet

It is vital for code executed at scale to perform well. It is crucial to ensure performance optimizations actually make the code faster. Luckily, we have powerful tools which help—BenchmarkDotNet is a .NET library for benchmarking optimizations, with plenty of simple examples to help get started.

In most systems, the code we need to optimize is rarely simple. It contains assumptions we need to discover before we even know what to improve. The code is hard to isolate. It has dependencies, which may or may not be relevant to optimization. And even when we've decided what to optimize, it's hard to reliably benchmark the before and after. Only measurement can tell us if our changes actually make things faster. Without them, we could even make things slower, without even realizing.

Understanding how to create benchmarks is the tip of the iceberg. In this talk, you'll also learn how to identify what to change, how to isolate code for benchmarking, and more. You'll leave with a toolkit of succinct techniques and the confidence to go ahead and optimize your code.

## Brainstorming

- how to make performance optimization actionable
- putting a practical process in-place to isolate components, measure + change + measure again, without breaking current behavior. Rinse and repeat
- combine that with something like a macro benchmark to see how the small changes can all add up to real-world improvements for users
- What parameters have an impact on what I want to benchmark
- What are reasonable values for those parameters that make we reasonably certain I have a good comparison baseline without unnecessarily exploding the runtime of the benchmark
- Can I do a series of quick runs to get a feel of the direction I'm heading vs when are longer runs important
- Optimize existing things until you hit the point of diminishing return. Exploring those limits makes you learn a ton about potential improvements for a new design
- Starting with an NSB endpoint and explore factors that are in place that have an impact on the throughput. Iterate through all aspects of the stack.
- Present a few best practices of benchmarking while talking about these concrete examples
- How do I avoid getting too much into profilers?

## Introduction

I remember the first time I started benchmarking my code changes to verify whether the things I thought might accelerate this code really made an impact. I had already seen quite a few Benchmarks written with Benchmark.NET and felt quite certain it wouldn't take long. Oh, I was wrong. I mean, writing the skeleton of the benchmark was indeed simple. The mind-boggling part was trying to figure out what should be taken into the benchmark, how to isolate the code without a crazy amount of refactoring, what should be deliberately cut away to make sure the changes envisioned are going in the right direction, and how to measure, change, and measure without burning away the allotted budget. But why even bother and go through all this hassle?

For code that is executed at scale, the overall throughput and memory characteristics are important. Code that wastes unnecessary CPU or memory cycles ends up eating away resources that could be used to serve requests. With modern cloud-native approaches, scalable code is even more important than before because we are often billed by the number of resources consumed. The more efficient the code is, the smaller the bill, or the more requests we can execute for the same amount of money.

In this talk, I have summarized my personal lessons on how to make performance optimizations actionable. I will show you a practical process to identify some common bottlenecks, isolate components, and measure + change + measure without breaking current behavior. Let's not waste more time and get to the essence of this talk.

## The performance loop

For me one of the key principles I try to apply to almost everything in software is making explicit tradeoffs and decisions as we go. This also applies to performance. A reasonably mature team should be "performance aware". My friend Maarten Balliauw once famously said, in some countries you have to be bear aware because for example, when you are hiking in Canada it is good to be prepared for the likelihood of a bear crossing your hiking paths, not so much in Switzerland though ;) I digress...

When it comes to performance, when you are performance aware, it doesn't mean you have to always go all the way in. Not at all. In fact, I always start with the simplest solutions that just work first and get some reasonably good test coverage in place. Once I have a working solution with good coverage, I start asking myself questions like:

- How is this code going to be executed at scale, and what would the memory characteristics be (gut feeling)
- Are there simple low-hanging fruits I can apply to accelerate this code?
- Are there things I can move away from the hot path by simply restructuring a bit my code?
- What part is under my control and what isn't really?
- What optimizations can I apply, and when should I stop?

I have covered some of these nuances further in my talk "Performance Tricks I learned from contributing to the Azure .NET SDK.". Once I have a better understanding of the context of the code, depending on the outcome, I start applying the following performance loop.

- Write a simple "sample" or harness that makes it possible to observe the component under inspection with a memory profiler and/or a performance profiler. The profiler snapshots give me an indication of the different subsystems at play, allowing me to make an explicit decision on what to focus on and what to ignore.
- Then I select the hot path, for example, the one responsible for the majority of allocations or the biggest slowdown (or where I feel I can make a good enough impact without sinking days and weeks into it). If the code path in question is not well covered, I try to get some tests in place to make sure my tweaks will not break the existing assumptions / behavior => it doesn't help when something is superfast but utterly wrong :)
- Then I experiment with the changes I have in mind and check whether they pass the tests. Once it functionally works, I put things into a performance harness
- To save time, I extract the code as well as possible into a dedicated repository and do a series of "short runs" to see if I'm heading in the right direction. Once I'm reasonably happy with the outcome, I do a full job run to verify the before and after.
- Then I ship this code and focus my attention on other parts

But enough of the overview of the process. Let's dive into a practical example.

## NServiceBus Pipeline

NServiceBus is the heart of a distributed system and the Particular Service Platform. It helps create systems that are scalable, reliable, and flexible. At its core, NServiceBus works by routing messages between endpoints. Messages are plain C# classes that contain meaningful data for the business process that is being modeled. Endpoints can be running in different processes on different machines, even at different times. NServiceBus makes sure that each message reaches its intended destination and is processed. NServiceBus accomplishes this by providing an abstraction over existing queuing technologies. While it's possible to work directly with queuing systems, NServiceBus provides extra features to make applications more reliable and scalable.

The most critical infrastructure piece inside an NServiceBus endpoint is the NServiceBus pipeline. The pipeline is the engine that makes sure all the required steps involved (serialization, deserialization, transactions, data access...) in sending or receiving messages are executed as efficiently as possible. As such, it is crucial for the pipeline to not get in the way of our customers' code.

![NServiceBus Pipeline Overview](PipelinePublishV6/Pipeline.jpg)

This is conceptually very similar to the ASP.NET Core middleware

![ASP.NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/index/_static/request-delegate-pipeline.png)

or expressed in code

```csharp
app.Use(async (context, next) => {
    // Do work that can write to the Response.
    await next();
    // Do logging or other work that doesn't write to the Response.
});
```

or as classes

```csharp
public class RequestCultureMiddleware {
    private readonly RequestDelegate _next;

    public RequestCultureMiddleware(RequestDelegate next) {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context) {
        // Do work that does something before
        await _next(context);
        // Do work that does something after
    }
}
```

in NServiceBus those pipeline / middleware elements are called behaviors and look like the following

```csharp
public class Behavior : Behavior<IIncomingLogicalMessageContext> {
    public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
    {
        // Do work that does something before
        await next();
        // Do work that does something after
    }
}
```

During the pipeline execution, there is a lot that is actually going on. For example, for an incoming message, the transport (e.g. Azure Service Bus, SQS, MSMQ...) pushes the raw data of messages to the pipeline. The pipeline will deserialize the payload. Based on the message type, it might resolve infrastructure such as message handlers from the dependency injection container and load data from the persistence selected by the user (e.g CosmosDB, SQL Server, DynamoDB...). There are bits and pieces that create OpenTelemetry traces, logs and much more. In essence, we have to somehow focus on parts that are relevant to us within the context we are optimizing for. We can achieve that by profiling.

### Profiling the pipeline

To get a good overview of the problem domain in front of us, it is vital to create a sample or harness that allows us to zoom in on the problem space. Since my goal is to optimize the pipeline invocation, I can look at the pipeline invocation with a tool like DotTrace from Jetbrains to get a good understanding of the performance bottlenecks and/or analyze the memory usage with a tool like DotMemory.

Below is an excerpt of such a harness. The harness sets up NServiceBus with a transport, a serializer and an InMemory persistence (to avoid unnecessary overhead that is currently not our focus). The harness has various points where I can take a snapshot to understand the memory characteristics of what's happening. In general, such a harness should adhere to the following guidelines:

- Compiled and executed under Release mode
- The harness should run for at least a few seconds and keep the overhead as small as possible to make sure it does not dominate the profile.
- It's recommended to disable Tiered JIT (`<TieredCompilation>false</TieredCompilation>`) (to avoid the need of warm up) and emit full symbols (`<DebugType>pdbonly</DebugType` and `<DebugSymbols>true</DebugSymbols>`) (not enabled by default for Release builds)


For example, it publishes 1000 events in parallel, which are then received by a handler that does nothing.

```csharp
var endpointConfiguration = new EndpointConfiguration("PublishSample");
endpointConfiguration.UseSerialization<JsonSerializer>();
var transport = endpointConfiguration.UseTransport<MsmqTransport>();
transport.Routing().RegisterPublisher(typeof(MyEvent), "PublishSample");
endpointConfiguration.UsePersistence<InMemoryPersistence>();
endpointConfiguration.EnableInstallers();
endpointConfiguration.SendFailedMessagesTo("error");

var endpointInstance = await Endpoint.Start(endpointConfiguration);

Console.WriteLine("Attach the profiler and hit <enter>.");
Console.ReadLine();

var tasks = new List<Task>(1000);
for (int i = 0; i < 1000; i++)
{
    tasks.Add(endpointInstance.Publish(new MyEvent()));
}
await Task.WhenAll(tasks);

Console.WriteLine("Publish 1000 done. Get a snapshot");
Console.ReadLine();
```

The handler code:

```csharp
public class MyEventHandler : IHandleMessages<MyEvent>
{
    public Task Handle(MyEvent message, IMessageHandlerContext context)
    {
        Console.WriteLine("Event received");
        return Task.CompletedTask;
    }
}
```

Let's take a look at the memory pressure of the publish operations.

![Pipeline publish memory overview](PipelinePublishV6/PipelineV6PublishMemoryOverview.png)

To understand the data presented in front of us, we require domain knowledge of the problem at hand. That knowledge helps to navigate through the maze of noise we might see. As we can see, there are numerous `byte[]`, `MemoryStream` and `StreamWriter` allocations that are quite "hefty".

Before we jump to conclusions, let's take a look at the receiving end. That's when the messages invoke the handler we saw previously.

![Pipeline receive memory overview](PipelinePublishV6/PipelineV6ReceiveMemoryOverview.png)

Lots of `byte[]`, `XmlTextReaderNodes` and Message extensions allocations.

![Pipeline behavior chain allocations](PipelinePublishV6/PipelineV6BehaviorChainZoomIn.png)

The stack trace is also massive, containing plenty of steps that clearly hide the actual pipeline operations like `MutateIncomingTransportMessageBehavior` or `UnitOfWorkBehavior`.

So, should we try to optimize those away? Well, ideally, yes, but in this specific case, there are a few things we have to take into account:

- The allocations are mostly coming from the MSMQ transport which has a diminishing user base. Most users eventually transition away from MSMQ to either SQL Server, RabbitMQ or a cloud transport like Azure Service Bus or Amazon SQS. Our efforts there might lead to allocation reductions only for a very limited segment of users.
- Another angle could be that we might not be transport experts. We already know that by making iterative gains on this hot path, we will end up with great improvements. But since every activity has to be weighted against building features and all the other activities, it might not be justifiable right now to ramp up knowledge in that area.
- Finally, our goal is to see what we can do to optimize the pipeline. The pipeline optimizations have great benefits for all users, independent of the transports.

When we look at an individual invocation (called `Behavior`) we see the following picture:

![Pipeline Behavior allocations](PipelinePublishV6/PipelineV6BehaviorZoomIn.png)

So let's focus on the `Behavior`, `BehaviorChain`, `Func<Task>`, `Func<IBehaviorContext, Task>` and `__DisplayClass**` allocations since they are coming from the pipeline invocation. Luckily, dotMemory also allows us to filter by namespace to get a better overview.

![Display class allocations in pipeline](PipelinePublishV6/PipelineV6StageForkAndDisplayClasses.png)

## Testing the pipeline

Luckily, there were existing acceptance tests in place. Those acceptance tests are executing whole NServiceBus scenarios end-to-end. So, if anything breaks while modifying the pipeline, it should be immediately captured by those tests. At the time, I added a few additional tests that I wouldn't call it Unit Test but more of a component test that verifies more than just a single class.

![Basic tests](PipelineOptimizations/PipelineTests.png)

## Improving the pipeline

Before I even started benchmarking the changes and trying to compare it , I started making some improvements to the code that are not really relevant for this talk. In case you are interested in what I did, you can read these blog posts on the particular blog:

- [10X faster execution with compiled expression trees](https://particular.net/blog/10x-faster-execution-with-compiled-expression-trees)
- [How we achieved 5X faster pipeline execution by removing closure allocations](https://particular.net/blog/pipeline-and-closure-allocations)

In essence, I applied a bunch of memory allocation optimization tricks that removed all of the `Behavior`, `BehaviorChain`, `Func<Task>`, `Func<IBehaviorContext, Task>` and `__DisplayClass**` allocations mentioned previously.

TODO: Maybe move this section below to later?

Let's take a look at the memory pressure of the publish operations.

![Pipeline publish memory overview](PipelinePublishV6/PipelineV6PublishMemoryOverviewOptimized.png)

![Pipeline receive memory overview](PipelinePublishV6/PipelineV6ReceiveMemoryOverviewOptimized.png)

![Pipeline behavior chain allocations](PipelinePublishV6/PipelineV6BehaviorChainZoomInOptimized.png)

![Pipeline behavior chain allocations](PipelinePublishV6/PipelineV6StageForkAndDisplayClassesOptimized.png)

## Benchmarking the pipeline

If you are lucky, the thing you want to benchmark might be a public method on some helper or utility without countless external dependencies. Then it is mostly simple because you can either have a benchmark project in the solution, reference the assembly in question, and start calling the method. In the worst case, you might need to add `InternalsVisibleTo` to give the benchmark project access to that helper or utility. So much for the theory. In practice, software is way messier than we like to admit. Components sometimes come with numerous dependencies. So, we can bite the bullet and just throw them all under a benchmark but then the gains you are trying to compare might get lost in the signal-to-noise ratio.

When I first faced this problem, I started looking for various approaches and ended up with a pragmatic but potentially slightly controversial one. I want to highlight that this approach worked well for me, and I think there is great value in it for others too, but as always, every approach comes with tradeoffs. Towards the end of the talk, I'm also going to do an outlook on preventing regressions in code, where I highlight another approach as an alternative.

The pragmatic approach I took was copy-pasting the existing relevant pipeline components and adjusting the source code to the bare essentials. For example, since the pipeline is quite composable, I can remove all existing behaviors and just have relevant behaviors in my test harness. Furthermore, the dependency injection container can be replaced with hard-coded assumptions by simply newing up relevant classes where needed. Actual IO-Operations have been replaced by simply returning completed tasks since those IO-operations are known to be hundreds or a thousand times slower anyway, and our goal is to remove all obstacles in the way of pipeline execution before even doing IO-Operations. Without going into many more details about the mechanics of the pipeline, here is what the folder structure looks like:

![Pipeline extraction folder structure](PipelineOptimizations/FolderStructure.png)

When writing Unit Tests, we ideally want to test all methods and properties of the given type. We also test both the happy and unhappy paths. The result of every Unit Test run is a single value: passed or failed.

Benchmarks are different. First and foremost, the result of a benchmark run is never a single value. It's a whole distribution, described with values like mean, standard deviation, min, max and so on. To get a meaningful distribution, the benchmark has to be executed many, many times. This takes a lot of time. The goal of benchmarking is to test the performance of all the methods that are frequently used (hot paths) and should be performant. The focus should be on the most common use cases, not edge cases.

How do we find those hot paths and the most common use cases?

Let's take a look at the benchmark.

```csharp
[Config(typeof(Config))]
public class PipelineExecution {
    class Config : ManualConfig  {
        public Config()
        {
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.ShortRun);
        }
    }


    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }


    [GlobalSetup]
    public void SetUp()  {
        behaviorContext = new BehaviorContext();

        pipelineModificationsBeforeOptimizations = new PipelineModifications();
        for (int i = 0; i < PipelineDepth; i++)
        {
            pipelineModificationsBeforeOptimizations.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(BaseLineBehavior), i.ToString(), b => new BaseLineBehavior()));
        }

        pipelineModificationsAfterOptimizations = new PipelineModifications();
        for (int i = 0; i < PipelineDepth; i++)
        {
            pipelineModificationsAfterOptimizations.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(BehaviorOptimization), i.ToString(), b => new BehaviorOptimization()));
        }

        pipelineBeforeOptimizations = new BaseLinePipeline<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsBeforeOptimizations);
        pipelineAfterOptimizations = new PipelineOptimization<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsAfterOptimizations);
    }

    [Benchmark(Baseline = true)]
    public async Task Before() {
        await pipelineBeforeOptimizations.Invoke(behaviorContext);
    }

    [Benchmark]
    public async Task After() {
        await pipelineAfterOptimizations.Invoke(behaviorContext);
    }
}
```

For the pipeline we have already mentioned, we want to verify the raw pipeline execution throughput. So, that is one of the scenarios we have encapsulated here in this benchmark. 

The above benchmark sets up the infrastructure part in the `[GlobalSetup]` (*) which makes sure the method marked with the attribute will be executed exactly once before running the benchmark for the first time. It is important to do it in the global setup because we are not interested in measuring the setup time of the pipeline yet.

The next question is what influences the pipeline execution throughput. What is known is that the pipeline can be dynamically extended "in-depth" by adding more behavior. So coming up with reasonable pipeline depths would be a good permutation for the pipeline execution benchmark. The `PipelineDepth` property does exactly that.

To get a good feeling of where we are heading, I configured the benchmark to do a `ShortRun`.

### Benchmark Best practices

- The benchmarks should follow the Single Responsibility Principle as other methods do. It means that a single benchmark should do a single thing.
- A benchmark should have no side effects. For example, adding values to an existing field means the list is growing with every benchmark invocation.
- To prevent dead code elimination, BenchmarkDotNet consumes the result returned from a benchmark and writes it to a volatile field. Alternatively, use the [Consumer](https://github.com/dotnet/BenchmarkDotNet/blob/master/src/BenchmarkDotNet/Engines/Consumer.cs) directly.
- BenchmarkDotNet does not require the user to provide the number of invocations per iteration. This value is determined by BenchmarkDotNet during the Pilot Experiment Stage, based on the IterationTime setting
- Be explicit.C# language features like implicit casting and var allow us to introduce invisible side effects to the benchmarks.


(*) If your benchmark requires a clean state for every invocation, you need to use the `[IterationSetup]` attribute. Unfortunately, just using the `[IterationSetup]` attribute is not enough to get stable results. You also need to make sure that the benchmark itself performs enough computations for a single invocation to run longer than 100ms. If you don't, your benchmark will be entirely invalid.

### Why Benchmark.NET

Benchmarking is really hard (especially microbenchmarking), you can easily make a mistake during performance measurements.
BenchmarkDotNet will protect you from the common pitfalls (even for experienced developers) because it does all the dirty work for you:

- BenchmarkDotNet does not require the user to provide the number of iterations and invocations per iteration, it implements a smart heuristic based on standard error and runs the benchmark until the results are stable.
- BenchmarkDotNet runs every benchmark in a separate process, process isolation allows for avoiding side effects. The more memory allocated by a given benchmark, the bigger the difference between in-proc vs out-proc execution.
- BenchmarkDotNet was designed to make accurate micro-benchmarks with repeatable results possible, to achieve that, it does many things, including overhead calculation and subtraction, warm up of the code, it consumes results to avoid dead code elimination.
- BenchmarkDotNet removes outliers by default

## More scenarios that are relevant

TBD

## Preventing regressions

The goal here was to show an approach that has worked well for me for a long time, even before the tooling matured. Once you have established a performance culture, it would be possible to go even a step further. Preventing regressions is a fundamental part of a good performance culture. The cheapest regression is one that does not get into the product.

Thanks to the guidance in [Preventing Regressions](https://github.com/dotnet/performance/blob/main/docs/benchmarking-workflow-dotnet-runtime.md) and the [ResultComparer](https://github.com/dotnet/performance/blob/main/src/tools/ResultsComparer/README.md) tool, it is possible to execute benchmarks against the baseline version of the code, store the artifacts in a dedicated folder (example `before`), forward the repository history to the optimized versions, build in release mode, execute the same benchmark again, but this time store the results in another folder (example `after`) and then compare them.

```bash
C:\Projects\performance\src\tools\ResultsComparer> dotnet run --base "C:\results\before" --diff "C:\results\after" --threshold 2%
```

## Recap

TBD

## Benchmark Pipeline (First iteration)

- [First iteration](https://github.com/Particular/NServiceBus/pull/4125)
- Explain isolation of the various moving pieces
- Talk a bit about the before and after pattern
- Talk about the cycle of improve, measure, improve
- Various settings like ShortRuns, DryRuns and some best practices

## Benchmark Pipeline (Second iteration)

- [Second iteration](https://github.com/Particular/NServiceBus/pull/6237)
- Show how we can iteratively improve things with this approach

- [Third iteration](https://github.com/Particular/NServiceBus/pull/6394)

## Talk about getting lower on the stack

- Transport Azure Service Bus
- AMQP
- Show how we can do various micro optimization that have a compounding effect until we reach the point of redesigning (example body refactoring)

## Azure Service Bus SDK

- Show some of the body optimization benchmarks (example <https://github.com/danielmarbach/MicroBenchmarks/tree/master/MicroBenchmarks/ServiceBus>) and the high level view shown in <https://github.com/Azure/azure-sdk-for-net/pull/19996#issuecomment-812663407>

## AMQP Level

- If possible to down to that level to talk about encoding optimizations (see <https://github.com/danielmarbach/azure-amqp-benchmarks>)

## Recap

- Recap the process of "putting a practical process in-place to isolate components, measure + change + measure again, without breaking current behavior. Rinse and repeat"
- Recap some of the Benchmark.NET rules but point for more information to the microbenchmark design guidelines

## Interesting further reading material

- [Intro to Benchmark.net - How To Benchmark C# Code](https://www.youtube.com/watch?v=mmza9x3QxYE)
- [Getting started with dotMemory](https://www.youtube.com/watch?v=6Tmcx6cTExg)
- [How to profile .NET Core applications with dotTrace](https://www.youtube.com/watch?v=ZWS156lKAos)
- [Performance Profiling with Visual Studio](https://www.youtube.com/watch?v=FpibK0PKfcI&list=PLReL099Y5nRf2cOurn1hI-gSRxsdbC27C)
- [Microbenchmark Design Guidelines](https://github.com/dotnet/performance/blob/main/docs/microbenchmark-design-guidelines.md)

