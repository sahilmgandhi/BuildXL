#!/bin/bash

protoc -I="Proto/" --python_out="Generated/" Proto/*.proto
