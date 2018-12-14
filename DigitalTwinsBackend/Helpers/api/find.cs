// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DigitalTwinsBackend.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DigitalTwinsBackend.Helpers
{
    public partial class Api
    {

        // Returns a Device with same hardwareId and spaceId if there is exactly one.
        // Or returns a Sensor with same hardwareId and deviceId if there is exactly one.
        // Otherwise returns null.
        public static async Task<T> FindElementAsync<T>(
            HttpClient httpClient,
            ILogger logger,
            string idFilter,
            string parentFilter,
            string includes = null)
        {
            //var filterHardwareIds = $"hardwareIds={hardwareId}";
            var includesParam = includes != null ? $"&includes={includes}" : "";
            var filter = $"{idFilter}&{parentFilter}{includesParam}";

            var response = await httpClient.GetAsync($"{typeof(T).Name.ToLower()}s?{filter}");
            if (await IsSuccessCall(response, logger))
            {
                var content = await response.Content.ReadAsStringAsync();
                var elements = JsonConvert.DeserializeObject<IReadOnlyCollection<T>>(content);
                var matchingElement = elements.SingleOrDefault();
                if (matchingElement != null)
                {
                    logger.LogInformation($"Retrieved Unique {typeof(T).Name} : {JsonConvert.SerializeObject(matchingElement, Formatting.Indented)}");
                    return matchingElement;
                }
            }
            return default(T);
        }




        // Returns a device with same hardwareId and spaceId if there is exactly one.
        // Otherwise returns null.
        //public static async Task<Models.Device> FindDevice(
        //    HttpClient httpClient,
        //    ILogger logger,
        //    string hardwareId,
        //    Guid? spaceId,
        //    string includes = null)
        //{
        //    var filterHardwareIds = $"hardwareIds={hardwareId}";
        //    var filterSpaceId = spaceId != null ? $"&spaceId={spaceId.ToString()}" : "";
        //    var includesParam = includes != null ? $"&includes={includes}" : "";
        //    var filter = $"{filterHardwareIds}{filterSpaceId}{includesParam}";

        //    var response = await httpClient.GetAsync($"devices?{filter}");
        //    if (response.IsSuccessStatusCode)
        //    {
        //        var content = await response.Content.ReadAsStringAsync();
        //        var devices = JsonConvert.DeserializeObject<IReadOnlyCollection<Models.Device>>(content);
        //        var matchingDevice = devices.SingleOrDefault();
        //        if (matchingDevice != null)
        //        {
        //            logger.LogInformation($"Retrieved Unique Device using 'hardwareId' and 'spaceId': {JsonConvert.SerializeObject(matchingDevice, Formatting.Indented)}");
        //            return matchingDevice;
        //        }
        //    }
        //    return null;
        //}

        //public static async Task<Models.Sensor> FindSensor(
        //    HttpClient httpClient,
        //    ILogger logger,
        //    string hardwareId,
        //    Guid? deviceId,
        //    string includes = null)
        //{
        //    var filterHardwareIds = $"hardwareIds={hardwareId}";
        //    var filterDeviceId = deviceId != null ? $"&deviceIds={deviceId.ToString()}" : "";
        //    var includesParam = includes != null ? $"&includes={includes}" : "";
        //    var filter = $"{filterHardwareIds}{filterDeviceId}{includesParam}";

        //    var response = await httpClient.GetAsync($"sensors?{filter}");
        //    if (response.IsSuccessStatusCode)
        //    {
        //        var content = await response.Content.ReadAsStringAsync();
        //        var sensors = JsonConvert.DeserializeObject<IReadOnlyCollection<Models.Sensor>>(content);
        //        var matchingSensor = sensors.SingleOrDefault();
        //        if (matchingSensor != null)
        //        {
        //            logger.LogInformation($"Retrieved Unique Sensor using 'hardwareId' and 'deviceId': {JsonConvert.SerializeObject(matchingSensor, Formatting.Indented)}");
        //            return matchingSensor;
        //        }
        //    }
        //    return null;
        //}

        // Returns a matcher with same name and spaceId if there is exactly one.
        // Otherwise returns null.
        public static async Task<IEnumerable<Models.Matcher>> FindMatchers(
            HttpClient httpClient,
            ILogger logger,
            IEnumerable<string> names,
            Guid spaceId)
        {
            var commaDelimitedNames = names.Aggregate((string acc, string s) => acc + "," + s);
            var filterNames = $"names={commaDelimitedNames}";
            var filterSpaceId = $"&spaceId={spaceId.ToString()}";
            var filter = $"{filterNames}{filterSpaceId}";

            var response = await httpClient.GetAsync($"matchers?{filter}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var matchers = JsonConvert.DeserializeObject<IReadOnlyCollection<Models.Matcher>>(content);
                if (matchers != null)
                {
                    logger.LogInformation($"Retrieved Unique Matchers using 'names' and 'spaceId': {JsonConvert.SerializeObject(matchers, Formatting.Indented)}");
                    return matchers;
                }
            }
            return null;
        }

        // Returns a space with same name and parentId if there is exactly one
        // that maches that criteria. Otherwise returns null.
        public static async Task<Space> FindSpace(
            HttpClient httpClient,
            ILogger logger,
            string name,
            Guid parentId)
        {
            var filterName = $"Name eq '{name}'";
            var filterParentSpaceId = parentId != Guid.Empty
                ? $"ParentSpaceId eq guid'{parentId}'"
                : $"ParentSpaceId eq null";
            var odataFilter = $"$filter={filterName} and {filterParentSpaceId}";

            var response = await httpClient.GetAsync($"spaces?{odataFilter}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var spaces = JsonConvert.DeserializeObject<IReadOnlyCollection<Models.Space>>(content);
                var matchingSpace = spaces.SingleOrDefault();
                if (matchingSpace != null)
                {
                    logger.LogInformation($"Retrieved Unique Space using 'name' and 'parentSpaceId': {JsonConvert.SerializeObject(matchingSpace, Formatting.Indented)}");
                    return matchingSpace;
                }
            }
            return null;
        }

        public static async Task<IEnumerable<Space>> FindSpaces(
            HttpClient httpClient,
            ILogger logger,
            string name,
            int typeId = -1)
        {
            var filterName = $"substringof('{name}', Name)";
            var filterTypeId = typeId != -1 ? $"and TypeId eq {typeId}" : "";

            var odataFilter = $"$filter={filterName} {filterTypeId}";

            var response = await httpClient.GetAsync($"spaces?{odataFilter}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IEnumerable<Models.Space>>(content);
            }
            return new List<Space>();
        }

        // Returns a user defined fucntion with same name and spaceId if there is exactly one.
        // Otherwise returns null.
        //public static async Task<Models.UserDefinedFunction> FindUserDefinedFunction(
        //    HttpClient httpClient,
        //    ILogger logger,
        //    string name,
        //    Guid spaceId)
        //{
        //    var filterNames = $"names={name}";
        //    var filterSpaceId = $"&spaceId={spaceId.ToString()}";
        //    var filter = $"{filterNames}{filterSpaceId}";

        //    var response = await httpClient.GetAsync($"userdefinedfunctions?{filter}&includes=matchers");
        //    if (response.IsSuccessStatusCode)
        //    {
        //        var content = await response.Content.ReadAsStringAsync();
        //        var userDefinedFunctions = JsonConvert.DeserializeObject<IReadOnlyCollection<Models.UserDefinedFunction>>(content);
        //        var userDefinedFunction = userDefinedFunctions.SingleOrDefault();
        //        if (userDefinedFunction != null)
        //        {
        //            logger.LogInformation($"Retrieved Unique UserDefinedFunction using 'name' and 'spaceId': {JsonConvert.SerializeObject(userDefinedFunction, Formatting.Indented)}");
        //            return userDefinedFunction;
        //        }
        //    }
        //    return null;
        //}


        public static async Task<IEnumerable<Models.Sensor>> FindSensorsOfSpace(
            HttpClient httpClient,
            ILogger logger,
            Guid spaceId)
        {
            var response = await httpClient.GetAsync($"sensors?spaceId={spaceId.ToString()}&includes=Types");
            if (await IsSuccessCall(response, logger))
            {
                var content = await response.Content.ReadAsStringAsync();
                var sensors = JsonConvert.DeserializeObject<IEnumerable<Models.Sensor>>(content);
                logger.LogInformation($"Retrieved {sensors.Count()} Sensors");
                return sensors;
            }
            else
            {
                return Array.Empty<Models.Sensor>();
            }
        }

    }
}
