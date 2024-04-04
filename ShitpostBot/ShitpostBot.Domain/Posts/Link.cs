using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using CSharpFunctionalExtensions;

namespace ShitpostBot.Domain;

public sealed class Link : ValueObject
{
    public string LinkId { get; private set; }
    public Uri LinkUri { get; private set; }
    public LinkProvider LinkProvider { get; private set; }

    private Link()
    {
        // For EF
    }

    internal Link(string linkId, Uri linkUri, LinkProvider linkProvider)
    {
        LinkId = linkId;
        LinkUri = linkUri;
        LinkProvider = linkProvider;
    }

    protected override IEnumerable<IComparable> GetEqualityComponents()
    {
        yield return LinkId;
        yield return LinkUri.ToString();
        yield return LinkProvider;
    }

    /// <summary>
    /// Returns null if link has no path (eg. www.google.com)
    /// </summary>
    /// <param name="linkUri"></param>
    /// <returns></returns>
    public static Link? CreateOrDefault(Uri linkUri)
    {
        LinkProvider linkProvider;
        string linkId;
        switch (linkUri.Host)
        {
            case "cdn.7tv.app":
            case "www.cdn.7tv.app":
            case "tenor.com":
            case "www.tenor.com":
            case "media.discordapp.net" when Path.GetExtension(linkUri.LocalPath) == ".gif":
            {
                return null;
            }
            case "github.com":
            case "www.github.com":
            {
                return null;
            }
            case "www.youtube.com":
            case "youtube.com":
            {
                linkProvider = LinkProvider.YouTube;
                linkId = HttpUtility.ParseQueryString(linkUri.Query)["v"]!;
                break;
            }
            case "www.youtu.be":
            case "youtu.be":
            {
                linkProvider = LinkProvider.YouTube;
                linkId = linkUri.LocalPath.Remove(0, 1);
                break;
            }
            case "www.steamcommunity.com" when linkUri.LocalPath == "/sharedfiles/filedetails/":
            case "steamcommunity.com" when linkUri.LocalPath == "/sharedfiles/filedetails/":
            {
                linkProvider = LinkProvider.SteamWorkshop;
                linkId = HttpUtility.ParseQueryString(linkUri.Query)["id"]!;
                break;
            }
            default:
            {
                linkProvider = LinkProvider.Generic;
                linkId = linkUri.LocalPath.Remove(0, 1);
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(linkId))
        {
            return null;
        }

        // TODO: better link whitelisting mechanism
        if (linkId == "9R4MtYRk6bA")
        {
            return null;
        }

        return new Link(linkId, linkUri, linkProvider);
    }
}

public enum LinkProvider
{
    Generic = 1,
    YouTube = 2,
    SteamWorkshop = 3
}
