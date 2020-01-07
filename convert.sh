# !/bin/sh

for i in *.cr3
    do sips -Z 1920 -s format png $i --out "${i%.*}.png"
done