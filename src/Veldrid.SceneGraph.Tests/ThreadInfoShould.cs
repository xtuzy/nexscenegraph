//
// Copyright 2018-2021 Sean Spicer 
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Veldrid.SceneGraph.Tests
{
    [TestFixture]
    public class ThreadInfoShould
    {
        [TestCase]
        public void DoesNotThrowCrossThreadExceptionIfAssertRenderingThreadThreadsDoNotMatch()
        {
            ThreadInfo.Instance.SetRenderingThreadCurrent();
            Task.Run(() =>
            {
                Assert.Throws<CrossThreadException>(() =>
                {
                    ThreadInfo.Instance.AssertRenderingThread();
                });
            });
        }
        
        [TestCase]
        public void NotThrowCrossThreadExceptionIfAssertRenderingThreadThreadsMatch()
        {
            ThreadInfo.Instance.SetRenderingThreadCurrent();

            Assert.DoesNotThrow(() =>
            {
                ThreadInfo.Instance.AssertRenderingThread();
            });
            
        }
    }
}