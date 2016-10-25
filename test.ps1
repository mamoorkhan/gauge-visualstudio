# Copyright [2014, 2015] [ThoughtWorks Inc.](www.thoughtworks.com)
# 
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
#     http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

if(!(Test-Path ".\artifacts"))
{
    Write-Host "No project artifacts found, invoking build"
    & ".\build.ps1"
}

$nunit = "$($pwd)\packages\NUnit.Console.3.0.1\tools\nunit3-console.exe"

if(!(Test-Path $nunit))
{
    throw "Nunit runner not found in $pwd"
}
&$nunit "$($pwd)\artifacts\Gauge.VisualStudio.Tests.dll" "$($pwd)\artifacts\Gauge.VisualStudio.Model.Tests.dll" "$($pwd)\artifacts\Gauge.VisualStudio.Core.Tests.dll" --result:"$($pwd)\artifacts\gauge.visualstudio.xml"

# Hack to break on exit code. Powershell does not seem to propogate the exit code from test failures.
if($LastExitCode -ne 0)
{
    throw "Test execution failed."
}