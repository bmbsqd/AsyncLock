@echo off

set outputPath=..\packages\bmbsqd-asyncLock

mkdir %outputPath%
nuget pack -Verbosity detailed -Symbols -Build -OutputDirectory %outputPath% -Prop Configuration=Release
