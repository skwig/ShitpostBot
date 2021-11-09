using System;
using FluentAssertions;
using NUnit.Framework;
using ShitpostBot.Domain;

namespace ShitpostBot.Tests
{
    [TestFixture]
    public class LinkTests
    {
        [TestCase("https://streamable.com/xb6yrr", LinkProvider.Streamable, "xb6yrr")]
        [TestCase("https://www.youtube.com/watch?v=eusx0VW-m3M", LinkProvider.YouTube, "eusx0VW-m3M")]
        [TestCase("https://www.youtube.com/watch?v=eusx0VW-m3M&t=60", LinkProvider.YouTube, "eusx0VW-m3M")]
        [TestCase("https://youtu.be/eusx0VW-m3M", LinkProvider.YouTube, "eusx0VW-m3M")]
        [TestCase("https://youtu.be/eusx0VW-m3M?t=60", LinkProvider.YouTube, "eusx0VW-m3M")]
        public void METHOD(string linkUri, LinkProvider expectedLinkProvider, string expectedLinkId)
        {
            var link = new Link(new Uri(linkUri));

            link.LinkProvider.Should().Be(expectedLinkProvider);
            link.LinkId.Should().Be(expectedLinkId);
        }
    }
}