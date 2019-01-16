// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using DigitalTwinsBackend.Helpers;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

namespace DigitalTwinsBackend.Models
{
    public class Matcher : BaseModel
    {
        public string Name { get; set; }
        public string SpaceId { get; set; }
        public IEnumerable<MatcherCondition> Conditions { get; set; }
        public IEnumerable<UserDefinedFunction> UserDefinedFunctions { get; set; }
        public override string Label { get { return Name; } }

        public Matcher()
        {
            Conditions = new List<MatcherCondition>();
        }

        public override Dictionary<string, object> ToCreate()
        {
            Dictionary<string, object> createFields = new Dictionary<string, object>();

            createFields.Add("Name", Name);
            createFields.Add("SpaceId", SpaceId);
            createFields.Add("Conditions", Conditions);

            return createFields;
        }

        public override Dictionary<string, object> ToUpdate(IMemoryCache memoryCache, out BaseModel updatedElement)
        {
            Dictionary<string, object> changes = new Dictionary<string, object>();

            Matcher oldValue = null;
            if (Id != Guid.Empty)
            {
                oldValue = CacheHelper.GetMatcherFromCache(memoryCache, Id);

                if (oldValue != null)
                {
                    if (Name != null && !Name.Equals(oldValue.Name)) changes.Add("Name", Name);
                    if (!SpaceId.Equals(oldValue.SpaceId)) changes.Add("SpaceId", SpaceId);
                    if (!Conditions.Equals(oldValue.Conditions)) changes.Add("Conditions", Conditions);
                    if (!UserDefinedFunctions.Equals(oldValue.UserDefinedFunctions)) changes.Add("UserDefinedFunctions", UserDefinedFunctions);
                }
                else
                {
                    changes.Add("Name", Name);
                    changes.Add("SpaceId", SpaceId);
                    changes.Add("Conditions", Conditions);
                    changes.Add("UserDefinedFunctions", UserDefinedFunctions);
                }
            }
            updatedElement = null;
            return changes;
        }
    }
}