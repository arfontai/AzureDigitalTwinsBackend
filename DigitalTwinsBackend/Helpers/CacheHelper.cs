﻿using DigitalTwinsBackend.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace DigitalTwinsBackend.Helpers
{
    public static class CacheKeys
    {
        public static string ObjectId { get { return "_WorkingObjectId"; } }
        public static string PreviousPage { get { return "_Context"; } }
        public static string InfoMessages { get { return "_InfoMessages"; } }
        public static string APICallMessages { get { return "_APICallMessages"; } }
        public static string SimulatedSensorList { get { return "_SimulatedSensorList"; } }
        public static string OntologyList { get { return "_OntologyList"; } }
        public static string HttpClient { get { return "_AuthenticationToken"; } }
        public static string IsInSendingDataState { get { return "_IsInSendingDataState"; } }
    }

    public enum Context
    {
        None,
        RootSpaces,
        Space,
        Device,
        Sensor,
        Matcher,
        UDF,
        PropertyKey,
        Blob
    }

    public class CacheHelper
    {
        //private static Context context;
        private static string id;

        private static string CacheKey(Context context)
        {
            //get
            {
                return (context == Context.None) ? id : id + "_" + context;
            }
        }

        #region Simulator related parameters & methods
        private static readonly string simulatorConfig = "Simulator.config";

        public static async Task<List<SimulatedSensor>> GetSimulatedSensorListFromCacheAsync(IMemoryCache memoryCache)
        {
            List<SimulatedSensor> list;

            if (!memoryCache.TryGetValue(CacheKeys.SimulatedSensorList, out list))
            {
                list = await LoadSimulatorConfigAsync();
                memoryCache.Set(CacheKeys.SimulatedSensorList, list);
            }
            return list;
        }

        public static async Task AddSimulatedSensorListInCacheAsync(IMemoryCache memoryCache, IEnumerable<SimulatedSensor> list)
        {
            memoryCache.Set(CacheKeys.SimulatedSensorList, list);
            await SaveSimulatorConfigAsync(list);
        }

        private static async Task<List<SimulatedSensor>> LoadSimulatorConfigAsync()
        {
            List<SimulatedSensor> list = new List<SimulatedSensor>();

            if (System.IO.File.Exists(simulatorConfig))
            {
                var content = await System.IO.File.ReadAllTextAsync(simulatorConfig);
                list = JsonConvert.DeserializeObject<List<SimulatedSensor>>(content);
            }
            return list;
        }

        private static async Task SaveSimulatorConfigAsync(IEnumerable<SimulatedSensor> list)
        {
            var listString = JsonConvert.SerializeObject(list);
            await System.IO.File.WriteAllTextAsync(simulatorConfig, listString);
        }

        public static bool IsInSendingDataState(IMemoryCache memoryCache)
        {
            bool isInSendingDataState;

            if (!memoryCache.TryGetValue(CacheKeys.IsInSendingDataState, out isInSendingDataState))
            {
                isInSendingDataState = false;
                memoryCache.Set(CacheKeys.IsInSendingDataState, isInSendingDataState);
            }
            return isInSendingDataState;
        }

        public static void SetInSendingDataState(IMemoryCache memoryCache, bool value)
        {
            memoryCache.Set(CacheKeys.IsInSendingDataState, value);
        }
        #endregion

        internal static void SetPreviousPage(IMemoryCache memoryCache, string previousPage)
        {
            memoryCache.Set(CacheKeys.PreviousPage, previousPage);
        }

        internal static string GetPreviousPage(IMemoryCache memoryCache)
        {
            Object _previousPage = null;
            memoryCache.TryGetValue(CacheKeys.PreviousPage, out _previousPage);

            if (_previousPage != null)
            {
                return _previousPage.ToString();
            }
            return "";
        }

        internal static void SetObjectId(IMemoryCache memoryCache, Guid objectId)
        {
            memoryCache.Set(CacheKeys.ObjectId, objectId);
        }

        internal static Guid GetObjectId(IMemoryCache memoryCache)
        {
            Object _objectId = null;
            memoryCache.TryGetValue(CacheKeys.ObjectId, out _objectId);

            if (_objectId != null)
            {
                return (Guid)_objectId;
            }
            return Guid.Empty;
        }

        internal static void AddInCache(IMemoryCache memoryCache, Object cacheElement, object key, Context context)
        {
            //context = scope;
            id = key.ToString();

            if (cacheElement != null)
            {

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(ConfigHelper.Config.parameters.CacheTime));

                memoryCache.Set(CacheKey(context), cacheElement, cacheEntryOptions);
            }
            else
            {
                memoryCache.Remove(CacheKey(context));
            }
        }

        internal static void ResetMessagesInCache(IMemoryCache memoryCache)
        {
            DeleteFromCache(memoryCache, CacheKeys.APICallMessages, Context.None);
            DeleteFromCache(memoryCache, CacheKeys.InfoMessages, Context.None);
        }

        internal static void AddAPICallMessageInCache(IMemoryCache memoryCache, string message)
        {
            List<string> messages = (List<string>)GetFromCache(memoryCache, CacheKeys.APICallMessages, Context.None);
            if (messages == null)
                messages = new List<string>();

            messages.Add(message);
            AddInCache(memoryCache, messages, CacheKeys.APICallMessages, Context.None);
        }

        internal static List<string> GetAPICallMessagesFromCache(IMemoryCache memoryCache)
        {
            return (List<string>)GetFromCache(memoryCache, CacheKeys.APICallMessages, Context.None);
        }

        internal static void AddInfoMessageInCache(IMemoryCache memoryCache, string message)
        {
            List<string> messages = (List<string>)GetFromCache(memoryCache, CacheKeys.InfoMessages, Context.None);
            if (messages == null)
                messages = new List<string>();

            messages.Add(message);
            AddInCache(memoryCache, messages, CacheKeys.InfoMessages, Context.None);
        }

        internal static List<string> GetInfoMessagesFromCache(IMemoryCache memoryCache)
        {
            return (List<string>)GetFromCache(memoryCache, CacheKeys.InfoMessages, Context.None);
        }

        internal static Object GetFromCache(IMemoryCache memoryCache, object key, Context context)
        {
            //context = scope;
            id = key.ToString();

            Object cacheElement = null;
            memoryCache.TryGetValue(CacheKey(context), out cacheElement);

            return cacheElement;
        }

        internal static void DeleteFromCache(IMemoryCache memoryCache, object key, Context context)
        {
            //context = scope;
            id = key.ToString();
            memoryCache.Remove(CacheKey(context));
        }

        public static async Task<HttpClient> GetHttpClientFromCacheAsync(IMemoryCache memoryCache, ILogger logger)
        {
            HttpClient httpClient;

            if (!memoryCache.TryGetValue(CacheKeys.HttpClient, out httpClient))
            {
                httpClient = await AuthenticationHelper.SetupHttpClient(logger);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(ConfigHelper.Config.parameters.CacheTime));
                memoryCache.Set(CacheKeys.HttpClient, httpClient, cacheEntryOptions);
            }
            return httpClient;
        }

        public static IEnumerable<Space> GetRootSpaceListFromCache(IMemoryCache memoryCache)
        {
            var spaces = GetFromCache(memoryCache, Guid.Empty, Context.RootSpaces);
            return spaces != null ? (IEnumerable<Space>)spaces : null;
        }

        public static IEnumerable<Space> GetSpaceListFromCache(IMemoryCache memoryCache)
        {
            var spaces = GetFromCache(memoryCache, Guid.Empty, Context.Space);
            return spaces != null ? (IEnumerable<Space>)spaces : null;
        }

        public static IEnumerable<Device> GetDeviceListFromCache(IMemoryCache memoryCache)
        {
            var devices = GetFromCache(memoryCache, Guid.Empty, Context.Device);
            return devices != null ? (IEnumerable<Device>)devices : null;
        }

        public static IEnumerable<UserDefinedFunction> GetUDFListFromCache(IMemoryCache memoryCache)
        {
            var udfs = GetFromCache(memoryCache, Guid.Empty, Context.UDF);
            return udfs != null ? (IEnumerable<UserDefinedFunction>)udfs : null;
        }

        public static IEnumerable<PropertyKey> GetPropertyKeyListFromCache(IMemoryCache memoryCache)
        {
            var propertyKeys = GetFromCache(memoryCache, Guid.Empty, Context.PropertyKey);
            return propertyKeys != null ? (IEnumerable<PropertyKey>)propertyKeys : null;
        }

        public static Space GetSpaceFromCache(IMemoryCache memoryCache, Guid id)
        {
            var space = GetFromCache(memoryCache, id, Context.Space);
            return space != null ? (Space)space : null;
        }

        public static IEnumerable<Matcher> GetMatchersFromCache(IMemoryCache memoryCache, Context context, Guid id)
        {
            var matchers = GetFromCache(memoryCache, id, context);
            return matchers != null ? (IEnumerable<Matcher>)matchers : null;
        }
               
        public static Matcher GetMatcherFromCache(IMemoryCache memoryCache, Guid id)
        {
            var matcher = GetFromCache(memoryCache, id, Context.Matcher);
            return matcher != null ? (Matcher)matcher : null;
        }

        public static IEnumerable<UserDefinedFunction> GetUDFsFromCache(IMemoryCache memoryCache, Context context, Guid id)
        {
            var udfs = GetFromCache(memoryCache, id, context);
            return udfs != null ? (IEnumerable<UserDefinedFunction>)udfs : null;
        }

        public static UserDefinedFunction GetUDFFromCache(IMemoryCache memoryCache, Guid id)
        {
            var udf = GetFromCache(memoryCache, id, Context.UDF);
            return udf != null ? (UserDefinedFunction)udf : null;
        }

        public static Device GetDeviceFromCache(IMemoryCache memoryCache, Guid id)
        {
            var device = GetFromCache(memoryCache, id, Context.Device);
            return device != null ? (Device)device : null;
        }

        public static Sensor GetSensorFromCache(IMemoryCache memoryCache, Guid id)
        {
            var sensor = GetFromCache(memoryCache, id, Context.Sensor);
            return sensor != null ? (Sensor)sensor : null;
        }

        public static IEnumerable<Models.Type> GetTypeListFromCache(Models.Types listType, IMemoryCache memoryCache)
        {
            var types = GetFromCache(memoryCache, listType, Context.None);
            return types != null ? (IEnumerable<Models.Type>)types : null;
        }

        public static IEnumerable<Ontology> GetOntologyListFromCache(IMemoryCache memoryCache)
        {
            var ontologies = GetFromCache(memoryCache, CacheKeys.OntologyList, Context.None);
            return ontologies != null ? (IEnumerable<Ontology>)ontologies : null;
        }

        public static IEnumerable<PropertyKey> GetPropertyKeysFromCache(IMemoryCache memoryCache, Context context, Guid spaceId)
        {
            var propertyKeys = GetFromCache(memoryCache, id, context);
            return propertyKeys != null ? (IEnumerable<PropertyKey>)propertyKeys : null;
        }

        public static PropertyKey GetPropertyKeyFromCache(IMemoryCache memoryCache, string id)
        {
            var propertyKey = GetFromCache(memoryCache, id, Context.PropertyKey);
            return propertyKey != null ? (PropertyKey)propertyKey : null;
        }

        public static IEnumerable<BlobContent> GetBlobContentsFromCache(IMemoryCache memoryCache, Guid spaceId)
        {
            var blobs = GetFromCache(memoryCache, spaceId, Context.Blob);
            return blobs != null ? (IEnumerable<BlobContent>)blobs : null;
        }
        
        public static BlobContent GetBlobContentFromCache(IMemoryCache memoryCache, Guid id)
        {
            var blob = GetFromCache(memoryCache, id, Context.Blob);
            return blob != null ? (BlobContent)blob : null;
        }
    }
}
