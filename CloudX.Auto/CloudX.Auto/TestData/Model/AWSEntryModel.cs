﻿using System.Collections.Generic;

namespace CloudX.Auto.Tests.TestData.Model
{
    public class AWSEntryModel
    {
        public AWSEntryModel()
        {
            Values = new List<string>();
        }

        public string Name { get; set; }
        public List<string> Values { get; set; }
    }
}
