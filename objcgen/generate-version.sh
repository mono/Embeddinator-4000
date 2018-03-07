#!/bin/bash -e

cd $(dirname $0)
make Make.config.inc -s

VERSION=$(cat Make.config.inc  | grep VERSION | cut -d '=' -f 2)
FILE=Version.generated.cs

echo "namespace Embeddinator.ObjC {" > $FILE
echo -e "\tstatic class Info {" >> $FILE
echo -e "\t\tpublic const string Version = \"$VERSION\";" >> $FILE
echo -e "\t\tpublic const string Hash = \"$(git log -1 --pretty=format:%h)\";" >> $FILE
echo -e "\t\tpublic const string Branch = \"$(git symbolic-ref --short HEAD)\";" >> $FILE
echo -e "\t}" >> $FILE
echo -e "}" >> $FILE
