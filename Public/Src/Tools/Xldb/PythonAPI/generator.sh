#!/bin/bash

PROTO_DIR=Proto/
GEN_DIR=Generated/

# Create the generated and protofile directories
echo "Creating directory"

rm -rf $PROTO_DIR
rm -rf $GEN_DIR

mkdir $PROTO_DIR
mkdir $GEN_DIR

echo "Done"

# Copy the Proto Files from Xldb.Proto
echo "Moving proto files from Xldb.proto"

cp ../../Xldb.Proto/*.proto $PROTO_DIR
cp ../../Xldb.Proto/Enums/*.proto $PROTO_DIR

echo "Done"

# Remove the google includes and the indirection from Proto Files
echo "Cleaning the proto files to work with python"

sed -i 's/tools\///g' ${PROTO_DIR}*.proto
sed -i 's/Enums\///g' ${PROTO_DIR}*.proto

echo "Done"

# Generates the Proto Files
echo "Creating generated Proto Files"

protoc -I="${PROTO_DIR}" --python_out="${GEN_DIR}" ${PROTO_DIR}*.proto

echo "Done"

# Create __init__.py and write the appropriate things into it
echo "Creating __init__.py with appropriate contents"

cd ${GEN_DIR}
PYFILE=__init__.py

rm -f $PYFILE
touch $PYFILE

echo 'import sys

sys.path.append("./Generated")' >> $PYFILE

for file in *.py; do
    fname=${file%.py}

    if [ ${fname} == "__init__" ]; then
        continue
    else
        echo "from . import ${fname}" >> $PYFILE
        echo "from ${fname} import *" >> $PYFILE
    fi
    
done

echo "Done"