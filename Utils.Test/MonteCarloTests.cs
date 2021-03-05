using NUnit.Framework;
using System.Collections.Generic;
using System;
using Levrum.Utils.MonteCarlo;
using System.Linq;

namespace Levrum.Utils.Test
{
    public class MonteCarloTest
    {

        Dictionary<int, int> Counts;
        List<int> Data;

        [SetUp]
        public void Setup()
        {
            Counts = new Dictionary<int, int>();
            for(int i = 0; i<100; i++)
            {
                Counts.Add(i, 0);
            }

            Data = new List<int>();
            Random rng = new Random();
            for (int i = 0; i < 1000; i++)
            {
                int nextInt = rng.Next(100);
                Counts[nextInt]++;
                Data.Add(nextInt);
            }
        }

        [Test]
        public void Test1()
        {
            //Random rng = new Random();

            EmpiricalDistribution<int> listDist = new EmpiricalDistribution<int>();
            listDist.CreateDistribution(Data);
            Dictionary<int, int> testCounts = new Dictionary<int, int>();
            foreach(int i in Data)
            {
                if (!testCounts.ContainsKey(i))
                {
                    testCounts.Add(i, 0);
                }
                testCounts[i]++;
            }

            bool pass = true;
            var distCounts = listDist.Counts;
            if (distCounts.Count != testCounts.Count)
            {
                Assert.Fail("List Counts created incorrectly");
            }
            foreach(var kvp in distCounts)
            {
                if (!testCounts.ContainsKey(kvp.Key) || testCounts[kvp.Key] != kvp.Value)
                {
                    pass = false;
                    break;
                }
            }
            Assert.IsTrue(pass);

            int dataSum = Data.Count;
            var testProbs = testCounts.ToDictionary(x => x.Key, x => (double)x.Value / dataSum);

            var distProbs = listDist.Distribution;
            if (distProbs.Count != testProbs.Count)
            {
                Assert.Fail("List Probabilities created incorrectly");
            }
            double probSum = 0;
            foreach(var kvp in distProbs)
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



            //EmpiricalDistribution<int> dist = new EmpiricalDistribution<int>();
            //dist.CreateDistribution(Counts);


        }
    }
}