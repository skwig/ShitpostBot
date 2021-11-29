using System;
using FluentAssertions;
using NUnit.Framework;
using ShitpostBot.Domain;

namespace ShitpostBot.Tests
{
    [TestFixture]
    public class LinkTests
    {
        [TestCase("https://streamable.com/xb6yrr", LinkProvider.Generic, "xb6yrr")]
        [TestCase("https://www.youtube.com/watch?v=eusx0VW-m3M", LinkProvider.YouTube, "eusx0VW-m3M")]
        [TestCase("https://www.youtube.com/watch?v=eusx0VW-m3M&t=60", LinkProvider.YouTube, "eusx0VW-m3M")]
        [TestCase("https://youtu.be/eusx0VW-m3M", LinkProvider.YouTube, "eusx0VW-m3M")]
        [TestCase("https://youtu.be/eusx0VW-m3M?t=60", LinkProvider.YouTube, "eusx0VW-m3M")]
        [TestCase("https://steamcommunity.com/sharedfiles/filedetails/?id=505736710", LinkProvider.SteamWorkshop, "505736710")]
        [TestCase("https://steamcommunity.com/sharedfiles/filedetails/?id=2067312913", LinkProvider.SteamWorkshop, "2067312913")]
        [TestCase("https://www.idnes.cz/hry/magazin/stars-of-blood-valve-zrusena-hra.A211104_215739_bw-magazin_oma", LinkProvider.Generic, "hry/magazin/stars-of-blood-valve-zrusena-hra.A211104_215739_bw-magazin_oma")]
        [TestCase("https://www.google.com", null, null)]
        [TestCase("https://www.google.com/", null, null)]
        public void CreateOrDefault(string linkUri, LinkProvider? expectedLinkProvider, string? expectedLinkId)
        {
            var link = Link.CreateOrDefault(new Uri(linkUri));
            
            link?.LinkProvider.Should().Be(expectedLinkProvider);
            link?.LinkId.Should().Be(expectedLinkId);
        }
    }
}