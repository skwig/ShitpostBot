using FluentAssertions;
using ShitpostBot.Domain;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class LinkTests
{
    [Theory]
    [InlineData("https://streamable.com/xb6yrr", LinkProvider.Generic, "xb6yrr")]
    [InlineData("https://www.youtube.com/watch?v=eusx0VW-m3M", LinkProvider.YouTube, "eusx0VW-m3M")]
    [InlineData("https://www.youtube.com/watch?v=eusx0VW-m3M&t=60", LinkProvider.YouTube, "eusx0VW-m3M")]
    [InlineData("https://youtu.be/eusx0VW-m3M", LinkProvider.YouTube, "eusx0VW-m3M")]
    [InlineData("https://youtu.be/eusx0VW-m3M?t=60", LinkProvider.YouTube, "eusx0VW-m3M")]
    [InlineData("https://steamcommunity.com/sharedfiles/filedetails/?id=505736710", LinkProvider.SteamWorkshop, "505736710")]
    [InlineData("https://steamcommunity.com/sharedfiles/filedetails/?id=2067312913", LinkProvider.SteamWorkshop, "2067312913")]
    [InlineData("https://www.idnes.cz/hry/magazin/stars-of-blood-valve-zrusena-hra.A211104_215739_bw-magazin_oma", LinkProvider.Generic, "hry/magazin/stars-of-blood-valve-zrusena-hra.A211104_215739_bw-magazin_oma")]
    [InlineData("https://www.google.com", null, null)]
    [InlineData("https://www.google.com/", null, null)]
    [InlineData("https://tenor.com/view/nodding-moon-creepy-gif-14222607", null, null)]
    [InlineData("https://media.discordapp.net/attachments/138031010951593984/905070007178911774/dontbelievehislies.gif", null, null)]
    public void CreateOrDefault(string linkUri, LinkProvider? expectedLinkProvider, string? expectedLinkId)
    {
        var link = Link.CreateOrDefault(new Uri(linkUri));

        link?.LinkProvider.Should().Be(expectedLinkProvider);
        link?.LinkId.Should().Be(expectedLinkId);
    }
}