all: build

build:
	for i in $(IOS_SAMPLES) $(MAC_SAMPLES); do (cd $$i && make); done

clean:
	for i in $(IOS_SAMPLES) $(MAC_SAMPLES); do (cd $$i; make clean); done

IOS_SAMPLES=

MAC_SAMPLES=\
mac/weather\
