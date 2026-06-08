#!/bin/bash
# Build script for Vercel deployment of ASP.NET Core MVC app
# Publish the project to the directory Vercel expects to serve

dotnet publish -c Release -o .vercel_build_output
