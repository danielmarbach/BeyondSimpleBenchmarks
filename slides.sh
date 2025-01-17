
rm -R ./images/
rm -R ./lib/
mkdir ./images
cp -R ~/Dropbox/Apps/Slides\ App/the-performance-loopa-practical-guide-to-profiling-and-benchmarking/* ./images/
cp -R ~/Dropbox/Apps/Slides\ App/the-performance-loopa-practical-guide-to-profiling-and-benchmarking.html ./index.html
cp -R ~/Dropbox/Apps/Slides\ App/lib ./lib
awk '{gsub(/the-performance-loopa-practical-guide-to-profiling-and-benchmarking/, "images"); print}' index.html > index2.html
mv -f index2.html index.html