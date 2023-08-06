
rm ./images/ -r
rm ./lib/ -r
mkdir ./images
cp ~/Dropbox/Apps/Slides\ App/beyond-simple-benchmarks-a-practical-guide-to-optimizing-code-with-benchmarkdotnet/* ./images/ -r
cp ~/Dropbox/Apps/Slides\ App/beyond-simple-benchmarks-a-practical-guide-to-optimizing-code-with-benchmarkdotnet.html ./index.html -r
cp ~/Dropbox/Apps/Slides\ App/lib ./lib -r
awk '{gsub(/beyond-simple-benchmarks-a-practical-guide-to-optimizing-code-with-benchmarkdotnet/, "images"); print}' index.html > index2.html
mv index2.html index.html -f