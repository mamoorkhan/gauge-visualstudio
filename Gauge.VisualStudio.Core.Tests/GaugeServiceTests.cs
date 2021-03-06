﻿// Copyright [2014, 2015] [ThoughtWorks Inc.](www.thoughtworks.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Text;
using FakeItEasy;
using Gauge.VisualStudio.Core.Exceptions;
using NUnit.Framework;

namespace Gauge.VisualStudio.Core.Tests
{
    [TestFixture]
    public class GaugeServiceTests
    {
        [Test]
        public void ShouldBeCompatibleWithGaugeGreaterThanGaugeMinVersion()
        {
            var curGaugeMinVersion = GaugeService.MinGaugeVersion;
            var testGaugeVersion = new GaugeVersion(string.Format("{0}.{1}.{2}", curGaugeMinVersion.Major,
                curGaugeMinVersion.Minor, curGaugeMinVersion.Patch + 1));
            var json = "{\"version\": \"" + testGaugeVersion +
                       "\",\"plugins\": [{\"name\": \"csharp\",\"version\": \"0.9.2\"},{\"name\": \"html-report\",\"version\": \"2.1.0\"}]}";
            var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));

            var gaugeProcess = A.Fake<IGaugeProcess>();
            A.CallTo(() => gaugeProcess.Start()).Returns(true);
            A.CallTo(() => gaugeProcess.StandardOutput).Returns(new StreamReader(outputStream));
            A.CallTo(() => gaugeProcess.StandardError).Returns(new StreamReader(errorStream));

            Assert.DoesNotThrow(() => GaugeService.AssertCompatibility(gaugeProcess));
        }

        [Test]
        public void ShouldBeCompatibleWithGaugeMinVersionForVs()
        {
            var curGaugeMinVersion = GaugeService.MinGaugeVersion;
            var json = "{\"version\": \"" + curGaugeMinVersion +
                       "\",\"plugins\": [{\"name\": \"csharp\",\"version\": \"0.9.2\"},{\"name\": \"html-report\",\"version\": \"2.1.0\"}]}";
            var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));

            var gaugeProcess = A.Fake<IGaugeProcess>();
            A.CallTo(() => gaugeProcess.Start()).Returns(true);
            A.CallTo(() => gaugeProcess.StandardOutput).Returns(new StreamReader(outputStream));
            A.CallTo(() => gaugeProcess.StandardError).Returns(new StreamReader(errorStream));

            Assert.DoesNotThrow(() => GaugeService.AssertCompatibility(gaugeProcess));
        }

        [Test]
        public void ShouldBeIncompatibleWithOldGaugeVersion()
        {
            const string json =
                "{\"version\": \"0.6.2\",\"plugins\": [{\"name\": \"csharp\",\"version\": \"0.9.2\"},{\"name\": \"html-report\",\"version\": \"2.1.0\"}]}";
            var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
            var curGaugeMinVersion = GaugeService.MinGaugeVersion;
            var expectedMessage = "This plugin works with Gauge " + curGaugeMinVersion +
                                  " or above. You have Gauge 0.6.2 installed. Please update your Gauge installation.\n" +
                                  " Run 'gauge version' from your command prompt for installation information.";

            var gaugeProcess = A.Fake<IGaugeProcess>();
            A.CallTo(() => gaugeProcess.Start()).Returns(true);
            A.CallTo(() => gaugeProcess.StandardOutput).Returns(new StreamReader(outputStream));
            A.CallTo(() => gaugeProcess.StandardError).Returns(new StreamReader(errorStream));

            var gaugeVersionIncompatibleException =
                Assert.Throws<GaugeVersionIncompatibleException>(() =>
                    GaugeService.AssertCompatibility(gaugeProcess));

            Assert.AreEqual(expectedMessage, gaugeVersionIncompatibleException.Data["GaugeError"]);
        }

        [Test]
        public void ShouldGetGaugeVersion()
        {
            const string json =
                "{\"version\": \"0.4.0\",\"plugins\": [{\"name\": \"csharp\",\"version\": \"0.7.3\"},{\"name\": \"html-report\",\"version\": \"2.1.0\"}]}";
            var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));

            var gaugeProcess = A.Fake<IGaugeProcess>();
            A.CallTo(() => gaugeProcess.Start()).Returns(true);
            A.CallTo(() => gaugeProcess.StandardOutput).Returns(new StreamReader(outputStream));
            A.CallTo(() => gaugeProcess.StandardError).Returns(new StreamReader(errorStream));

            var installedGaugeVersion = GaugeService.GetInstalledGaugeVersion(gaugeProcess);
            Assert.AreEqual("0.4.0", installedGaugeVersion.version);
            Assert.AreEqual(2, installedGaugeVersion.plugins.Length);
        }

        [Test]
        public void ShouldGetGaugeVersionWhenDeprecated()
        {
            var json = string.Concat(
                "[DEPRECATED] This usage will be removed soon. Run `gauge help --legacy` for more info.",
                Environment.NewLine,
                "{\"version\": \"0.4.0\",\"plugins\": [{\"name\": \"csharp\",\"version\": \"0.7.3\"},{\"name\": \"html-report\",\"version\": \"2.1.0\"}]}");
            var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));

            var gaugeProcess = A.Fake<IGaugeProcess>();
            A.CallTo(() => gaugeProcess.Start()).Returns(true);
            A.CallTo(() => gaugeProcess.StandardOutput).Returns(new StreamReader(outputStream));
            A.CallTo(() => gaugeProcess.StandardError).Returns(new StreamReader(errorStream));

            var installedGaugeVersion = GaugeService.GetInstalledGaugeVersion(gaugeProcess);
            Assert.AreEqual("0.4.0", installedGaugeVersion.version);
            Assert.AreEqual(2, installedGaugeVersion.plugins.Length);
        }

        [Test]
        public void ShouldThrowExceptionWhenExitCodeIsNonZero()
        {
            const string errorMessage = "This is an error message";
            var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
            var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));

            var gaugeProcess = A.Fake<IGaugeProcess>();
            A.CallTo(() => gaugeProcess.Start()).Returns(true);
            A.CallTo(() => gaugeProcess.ExitCode).Returns(123);
            A.CallTo(() => gaugeProcess.StandardOutput).Returns(new StreamReader(outputStream));
            A.CallTo(() => gaugeProcess.StandardError).Returns(new StreamReader(errorStream));

            var exception =
                Assert.Throws<GaugeVersionNotFoundException>(() =>
                    GaugeService.GetInstalledGaugeVersion(gaugeProcess));

            Assert.NotNull(exception);
            Assert.NotNull(exception.Data);
            Assert.AreEqual("Unable to read Gauge version", exception.Message);
            Assert.AreEqual(errorMessage, exception.Data["GaugeError"]);
        }
    }
}