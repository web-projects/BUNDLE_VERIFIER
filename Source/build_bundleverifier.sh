#!/bin/bash

###################################################################
# build_bundleverifier.sh
#
# run from git bash as: 
# $ bash build_bundleverifier.sh
#
###################################################################

###################################################################
# Clean the entire solution
###################################################################
echo "** CLEANING THE SOLUTION **"
dotnet clean


###################################################################
# Rebuild BundleVersion in RELEASE mode
###################################################################
echo "** BUILDING SOLUTION IN RELEASE MODE **"
dotnet build --configuration Release
