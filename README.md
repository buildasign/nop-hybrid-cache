# nop-hybrid-cache
A Hybrid Cache for NopCommerce


﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿Overview
========
This repository contains the source code for a NopCommerce plugin (currently version 4.20), that enables Hybrid cache for the NopCommerce platform when running under a web farm scenario.
Hybrid cache is a combination of fast local memory cache on the web server, synchronized with a Redis back-end cache.
This is achieved via the EasyCaching Hybrid library (https://easycaching.readthedocs.io/en/latest/Hybrid/).

This plugin is provided to the community by BuildASign, llc. as is. Use at your own risk. BuildASign accepts no liability for the use of this plugin.

The main advantage of this type of cache is that it allows for the same performance (or better) for the web farm servers, while keeping themselves synchronized via a back-end Redis cache, which is the main problem when running NopCommerce in a web farm scenario.
It further adds more customization options than the default NopCommerce cache in order to tweak the size of items in cache than the default 10,000 items. If you have more than 10K items in cache at any point in time you may be operating at a cache deficiency as items will be constantly ejected as the cache reaches capacity.

Development
===========
This project assumes that you have your NopCommerce repository in a folder parallel to the Hybrid Cache plugin.
e.g. if you have this Hybrid Cache plugin repository downloaded to a c:\github\nop-hybrid-cache folder, ensure you have your NopCommerce folder at c:\github\nopcommerce.
Adjust accordingly to your development standards.
(i.e. if this is not where you have your NopCommerce code, edit the paths on the Nop.Plugin.Misc.HybridCache.csproj file where the Nop projects are referenced, or simply add the project to your NopCommerce solution, uncheck the Nop references and re-add them.)

You will also need to append the following settings to your appsettings.json file:
~~~~
  "CacheBackplane": {
    "cachelimit": 500000,
    "enablelogging": "false",
    "configuration": "127.0.0.1:6379,connectRetry=3,connectTimeout=5000,ssl=false,syncTimeout=30000,defaultDatabase=10,allowAdmin=true",
    "busconfiguration": "127.0.0.1:6379,connectRetry=3,connectTimeout=5000,ssl=false,syncTimeout=30000,defaultDatabase=11",
    "minIoThreads": 100,
    "minWorkerThreads": 100,
    "pageSize":  200
  }
~~~~
- cachelimit: Adjust according to your load - this overrides the default of 10,000 that NopCommerce gives you by default. Test your load to find out how many items you are throwing in cache.
- enablelogging: This is a pass-through property for EasyCaching - check their documentation on how to use
- configuration: This is the connection string for the Redis instance you wish to use. Set your database to whatever you wish.
- busconfiguration: This is the configuration string for the Redis instance you wish to use for the bus communication that sends messages to clear local caches on the web servers that are subscribed to this HybridCache. Set the database number to one above whatever you entered in the configuration above.
- minIoThreads/minWorkerThreads: this is a performance tweak for Redis. As you test your load if you see problems/slowness, you will need to see the error messages that Redis provides you. Tweak the minIoThreads/minWorkerThreads accordingly, if this is indicated as a problem. Use these articles to troubleshoot your load (https://azure.microsoft.com/en-us/blog/investigating-timeout-exceptions-in-stackexchange-redis-for-azure-redis-cache/ and https://docs.microsoft.com/en-us/azure/azure-cache-for-redis/cache-troubleshoot-client)
- pageSize: this is an optional parameter if you wish to modify the EasyCaching library. This tweaks the size of the pages when Redis runs SCAN commands, which it does when doing searches by prefix (when it clears caches by prefix, for example.) To reduce the amount of SCANS it does, you can gain a performance advantage by customizing the page size from the default.

Build
=====
You may build this code according to your development practices. You can build via Visual Studio, dotnet command line, Jenkins/Teamcity build process or whatever suits your fancy.

Requirements
============
.NET Core SDK 2.2.402 (https://dotnet.microsoft.com/download/dotnet-core/2.2)

You can check the currently installed versions using the below command:

~~~~
dotnet --list-sdks
~~~~

NopCommerce 4.20 (https://github.com/nopSolutions/nopCommerce/tree/release-4.20)

A Redis instance, either local or remote. This project assumes you have a local instance for local development, but if you don't, update your appsettings.json accordingly.

Notes on EasyCaching
====================
The version of EasyCaching for .Net Core 2.2 (0.5.6) had a few bugs that were fixed in subsequent versions. In order to take full advantage of the performance of Redis we had to make a few tweaks to some of  their libraries.
We cannot provide you with these versions as we don't own the EasyCaching library.
If you wish to take full advantage you will have to make your own version yourself.
But here is a list of the changes we made to tweak the most performance out of it that we could:
1. In the HybridCachingProvider, the local cache was never populated with the contents of the distributed cache, causing it to always use distributed cache (line 472)
2. In the HybridCachingProvider lines 126 and 138, it was always checking the Distributed Cache, causing many EXISTS calls to Redis, when the local cache could have the item. Changed the calls check local cache first, and only if not found to then look in distributed cache
3. The Nop memory manager has a Flush option to clear cache. In the HybridCachingProvider there was no Flush call, even to this day. Added a call to flush the cache so all items can be cleared, instead of doing searches by prefix to clear mass amounts of items, which is not performant.
4. In EasyCaching.Serialization.Json library, JsonOptionsExtension, added an overload to accept a JsonSerializerSettings action so that we can pass a custom serializer. See later versions of the library and copy the code over.
5. Similarly, in EasyCachingOptionsExtension, there needs to be support for the custom JsonSerializerSettings.
6. And finally, in the DefaultJsonSerializer, your need a constructor that takes in a JsonSerializerSettings. Again, see the later versions of EasyCaching to see how that is done.
7. EasyCaching.Redis - DefautRedisCachingProvider: line 483 was modified. Added option to to accept a pagesize parameter in order to minimize the amount of SCANS issued to Redis when searching by prefix. The default pagesize is 10. But if you have too many keys with the same prefix it will hurt performance. See the Redis SCAN command to get an idea of what this is about. See https://github.com/dotnetcore/EasyCaching/pull/199 for more information. From this redis dev specification, https://yq.aliyun.com/articles/531067 , maybe the appropriate scope is 100~500. By making it configurable, you can tweak according to your own load.
This was done by adding the PageSize option to the RedisDBOptions class.
