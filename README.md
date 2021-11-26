Scarlet
=======
__Scarlet__ and its related libraries are aiming to provide functionality to convert, export and import various types of game data. They are written in C# and based on .NET Standard and .NET (Core).

Disclaimer
==========
This project is still incomplete and work-in-progress. Functionality will change, be added or removed, and all interfaces, calling conventions, etc. should be considered subject to change. It has also been upgraded by me (CaptainSwag101/jpmac26) to the newer .NET Standard platform, which may cause unforseen bugs but also hopefully provide better performance and long-term support into the future.

I (CaptainSwag101/jpmac26) maintain this fork of Scarlet purely for my own purposes, and I do not offer any kind of support. I have gathered together various changes from other people's forks in the hopes that this may be the ideal branch of Scarlet for others to use, but my own knowledge of its inner workings means I cannot fix arbitrary issues without serious investigation. Therefore, your use of this fork is at your own risk.

Requirements
============
* General
  * [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
* Compilation
  * Visual Studio Community 2022 (or higher)

Parts
=====
* __Scarlet__: Common main library (required)
* __Scarlet.IO.ImageFormats__: Library for image file format conversion (requires Scarlet)
* __Scarlet.IO.ContainerFormats__: Library for container/archive management (requires Scarlet)
* __Scarlet.IO.CompressionFormats__: Library for handling compressed data (requires Scarlet)
* __ScarletTestApp__ (ScarletConvert): Sample converter application implementation (requires all the above)

Formats
=======
File formats that can be loaded and exported/extracted by the libraries as of [this commit](https://github.com/xdanieldzd/Scarlet/tree/30ff7d03987ced0613344a57d39cccf98f728ccf) contain the following:

* __Images__
  * BTGA (ex. various Lego games, 3DS)
  * CTXB (various Nintendo 3DS games)
  * DDS (DXTx and PVRx; ex. Skullgirls 2nd Encore, PS Vita)
  * DMPBM (ex. Shin Megami Tensei: Devil Survivor Overclocked)
  * GBIX (ex. K-ON! Houkago Live, PSP version)
  * GXT (various PlayStation Vita games)
  * KSLT (ex. Dead or Alive Xtreme 3: Venus, possibly more Koei Tecmo games)
  * NMT (ex. Disgaea 4, PS Vita version, possibly more Nippon Ichi Software games)
  * SHTX (ex. Danganronpa Another Episode)
  * SHTXFS (ex. Danganronpa Another Episode)
  * STEX (ex. Etrian Odyssey IV, Shin Megami Tensei IV, possibly more Atlus games)
  * TEX (various Capcom games)
  * TID (ex. Hyperdimension Neptunia ReBirth 1, PC _and_ PS Vita versions, possibly more Idea Factory/Compile Heart/Felistella games)
  * TIPS (ex. Uchuu no Stellvia, PS2 version; image-type TIPS files only)
  * TMX (various Atlus games)
  * TX2 (ex. Phantom Brave, PS2 version, various other Nippon Ichi Software games)
  * TXF (ex. Disgaea 4, PS3 version, possibly more Nippon Ichi Software games)
  * TXG (ex. Sakurasou no Pet na Kanojo, PSP version)
    * XGTL (wrapper around multiple TXGs)
      * CBG (wrapper around XGTL)
  * TXP (ex. Z.H.P: Unlosing Ranger vs Darkdeath Evilman, Disgaea 2 PSP, Disgaea Infinite, possibly more Nippon Ichi Software games on PSP)
  * VTXP (ex. Punchline, PS Vita; _not_ the same as, nor related to TXP above)
* __Containers__
  * FADEBABE (ex. Akiba's Trip, original PSP version, namely DATA1.DAT)
  * FMDX (ex. K-ON! Houkago Live, PSP version)
  * GAR v2 and v5 (ex. The Legend of Zelda: Majora's Mask 3D, Ever Oasis)
  * NISPACK (various Nippon Ichi Software games)
  * NSAC (ex. Disgaea 4, PS Vita version, possibly more Nippon Ichi Software games)
  * PSPFS_V1 (ex. Phantom Brave, PSP version, possibly more Nippon Ichi Software games)
  * Stellvia DATs (Uchuu no Stellvia, PS2 version)
  * UKArc/PAC (ex. Dengeki Bunko: Fighting Climax, PS Vita version)
  * ZAR v1 (ex. The Legend of Zelda: Ocarina of Time 3D)
* __Compression__
  * DR 1/2/AE (Danganronpa 1, 2 and Another Episode, PS Vita)
  * Grezzo LzS (ex. The Legend of Zelda: Majora's Mask 3D)
  * GZip (generic; also ex. Dengeki Bunko: Fighting Climax, PS Vita version)
  * NIS LZS (ex. Disgaea 4, PS3 and PS Vita versions)
  * Nintendo DS LZSS-0x10 (generic; also ex. Shin Megami Tensei: Devil Survivor Overclocked)

Note that support for these is not 100% complete (especially Capcom TEX is lacking), as well as the unintentional bias towards NIS games.

Acknowledgements
================
* PVRTC texture decompression code ported from [PowerVR Graphics Native SDK](https://github.com/powervr-graphics/Native_SDK), Copyright (c) Imagination Technologies Ltd.
  * see *\Scarlet\Drawing\Compression\PVRTC.cs* and *LICENSE.md*
* BC7 texture decompression code ported from [detex](https://github.com/hglm/detex) by Harm Hanemaaijer
  * see *\Scarlet\Drawing\Compression\BC7.cs* and *LICENSE.md*
* Includes [NetRevisionTool](http://unclassified.software/apps/netrevisiontool) by Yves Goergen for injecting Git revision information
* Initial VTXP and BTGA format notes and original Danganronpa decompression code by [BlackDragonHunt](https://github.com/BlackDragonHunt)
* Texture swizzle logic reverse-engineering and original C implementation by [FireyFly](https://github.com/FireyFly)
* ZAR v1 and GAR v2 container support adapted from documentation and/or code by Twili and [ShimmerFairy](https://github.com/ShimmerFairy) respectively
* Sample files, testing and moral support by [Ehm](https://twitter.com/OtherEhm)
* Support for Danganronpa: Another Episode texture formats by [Liquid-S](https://github.com/Liquid-S)
* Support for UBC4, SBC4, and UBC5 variants of GTX by [Cri4Key](https://github.com/Cri4Key)
* Fixes for DXT's TID compression flag by [尚嘉宣](https://github.com/shangjiaxuan)
* PS4 Swizzling support by [TGEnigma](https://github.com/TGEnigma), with fixes by CaptainSwag101 (jpmac26)
