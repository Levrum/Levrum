using NUnit.Framework;
using System.Collections.Generic;
using System;
using Levrum.Utils.MonteCarlo;
using System.Linq;

namespace Levrum.Utils.Test
{
    public class MonteCarloTest
    {

        Dictionary<int, int> zeroCounts;
        List<int> Data;

        private class FakeCall
        {
            public FakeCall(int code)
            {
                this.code = code;
            }
            public string name { get; set; }
            public int code { get; set; }
        }

        [SetUp]
        public void Setup()
        {
            zeroCounts = new Dictionary<int, int>();
            for (int i = 0; i < 100; i++)
            {
                zeroCounts.Add(i, 0);
            }

            Data = new List<int>();
            Random rng = new Random();
            for (int i = 0; i < 1000; i++)
            {
                int nextInt = rng.Next(100);
                zeroCounts[nextInt]++;
                Data.Add(nextInt);
            }
        }

        private bool TestDistribution(EmpiricalDistribution<int> testDistribution, Dictionary<int, int> trueCounts)
        {
            bool pass = true;
            var distCounts = testDistribution.Counts;
            if (distCounts.Count != trueCounts.Count)
            {
                Assert.Fail("List Counts created incorrectly");
            }
            foreach (var kvp in distCounts)
            {
                if (!trueCounts.ContainsKey(kvp.Key) || trueCounts[kvp.Key] != kvp.Value)
                {
                    pass = false;
                    break;
                }
            }
            Assert.IsTrue(pass);

            int dataSum = Data.Count;
            var testProbs = trueCounts.ToDictionary(x => x.Key, x => (double)x.Value / dataSum);

            var distProbs = testDistribution.Distribution;
            if (distProbs.Count != testProbs.Count)
            {
                Assert.Fail("List Probabilities created incorrectly");
            }
            double probSum = 0;
            foreach (var kvp in distProbs)
            {   //this is gonna get a little weird
                //the distribution has key=prob,value=value and the test probabilities are key=value,value=prob
                int value = kvp.Item2; //get the actual value
                double prob = kvp.Item1; //get the probability
                double testProb = testProbs[value]; //get the test generated probability
                double currProb = Math.Round(prob - probSum, 5);
                if (currProb != testProb) //since the actual probability is a sum of previous probabilities, subtract the current sum
                {
                    pass = false;
                    break;
                }
                probSum += testProb; //add the value to the sum for the next run


            }

            Assert.IsTrue(pass);

            if (Math.Round(probSum, 6) != 1.0)
            {
                Assert.Fail("didn't equal 100%");
            }
            return true;
        }

        [Test]
        public void TestCreateFromCalls()
        {
            Dictionary<int, int> trueCounts = new Dictionary<int, int>();
            List<FakeCall> callData = new List<FakeCall>();
            foreach (int i in Data)
            {
                if (!trueCounts.ContainsKey(i))
                {
                    trueCounts.Add(i, 0);
                }
                trueCounts[i]++;
                callData.Add(new FakeCall(i));
            }
            EmpiricalDistribution<int> testDist = new EmpiricalDistribution<int>();
            testDist.CreateDistribution(new List<object>(callData), x =>
            {
                var fCall = x as FakeCall;
                return fCall.code;
            });
            Assert.IsTrue(TestDistribution(testDist, trueCounts));
        }

        [Test]
        public void TestSample()
        {
            Dictionary<int, int> fakeData = new Dictionary<int, int>();
            fakeData.Add(1, 2); //20%
            fakeData.Add(2, 4); //40%
            fakeData.Add(3, 3); //30%
            fakeData.Add(4, 1); //10%

            EmpiricalDistribution<int> toSample = new EmpiricalDistribution<int>();
            toSample.CreateDistribution(fakeData);
            //dist should look like: (.1, 4), (.3, 1), (.6, 3), (1, 2)

            int shouldBeFour = toSample.GetValue(.05);
            Assert.AreEqual(shouldBeFour, 4);
            shouldBeFour = toSample.GetValue(.1);
            Assert.AreEqual(shouldBeFour, 4);

            int shouldBeOne = toSample.GetValue(.11);
            Assert.AreEqual(shouldBeOne, 1);
            shouldBeOne = toSample.GetValue(.3);
            Assert.AreEqual(shouldBeOne, 1);

            int shouldBeThree = toSample.GetValue(.304);
            Assert.AreEqual(shouldBeThree, 3);

            int shouldBeTwo = toSample.GetValue(.7);
            Assert.AreEqual(shouldBeTwo, 2);

            shouldBeTwo = toSample.GetValue(1);
            Assert.AreEqual(shouldBeTwo, 2);




        }

        [Test]
        public void Test1()
        {
            //Random rng = new Random();

            EmpiricalDistribution<int> listDist = new EmpiricalDistribution<int>();
            listDist.CreateDistribution(Data);
            Dictionary<int, int> testCounts = new Dictionary<int, int>();
            foreach (int i in Data)
            {
                if (!testCounts.ContainsKey(i))
                {
                    testCounts.Add(i, 0);
                }
                testCounts[i]++;
            }

            Assert.IsTrue(TestDistribution(listDist, testCounts));

        }
    }
}