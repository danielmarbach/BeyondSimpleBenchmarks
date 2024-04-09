
rm -R ./images/
rm -R ./lib/
mkdir ./images
cp -R ~/Dropbox/Apps/Slides\ App/beyond-simple-benchmarks-a-practical-guide-to-optimizing-code-with-benchmarkdotnet/* ./images/
cp -R ~/Dropbox/Apps/Slides\ App/beyond-simple-benchmarks-a-practical-guide-to-optimizing-code-with-benchmarkdotnet.html ./index.html
cp -R ~/Dropbox/Apps/Slides\ App/lib ./lib
awk '{gsub(/beyond-simple-benchmarks-a-practical-guide-to-optimizing-code-with-benchmarkdotnet/, "images"); print}' index.html > index2.html
mv -f index2.html index.html