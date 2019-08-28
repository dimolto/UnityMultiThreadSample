#!/bin/sh

for num in `seq 1 $1`
do
    cp default.txt ./sourceDirectory/default$num.txt
done
