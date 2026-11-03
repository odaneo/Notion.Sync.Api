using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Notion.Sync.Api.Sync;
using Xunit;

namespace Notion.Sync.Api.Tests;

public class SnapshotTests
{
    internal static readonly SyncOptions Settings = new() { Repository = "test/content", Branch = "main", Token = new string('t', 48) };
    internal static readonly DateTimeOffset Edited = DateTimeOffset.Parse("2026-11-03T01:00:00Z");
    internal const string ArticleId = "11111111111141118111111111111111";
    internal const string TagId = "22222222222242228222222222222222";
    internal const string SubTagId = "33333333333343338333333333333333";

    internal static CategoryInput Category(string id, string path, string slug) => new()
    { Id = id, SourcePath = path, Slug = slug, LastEditedTime = Edited };
    internal static ArticleInput Article(string content = "# Markdown", string? id = null, List<string?>? tags = null, List<string?>? subTags = null,
        string slug = "example", string sourcePath = "知识/示例.md") => new()
    {
        Published = true, Id = id ?? ArticleId, SourcePath = sourcePath, Slug = slug,
        Content = content, LastEditedTime = Edited, TagRefs = tags ?? ["[[后端]]"], SubTags = subTags ?? ["[[CSharp]]"]
    };
    internal static SyncRequest Request(long number = 1, string? sha = null, List<ArticleInput>? articles = null,
        List<CategoryInput>? tags = null, List<CategoryInput>? subTags = null, bool complete = true) => new()
    {
        Repository = Settings.Repository, Branch = Settings.Branch, CommitSha = sha ?? number.ToString("x").PadLeft(40, '0'),
        WorkflowRunNumber = number, Complete = complete, Articles = articles ?? [Article()],
        Tags = tags ?? [Category(TagId, "Tags/后端.md", "backend")],
        SubTags = subTags ?? [Category(SubTagId, "SubTags/CSharp.md", "c-sharp")]
    };

    [Fact]
    public void LinkFormsResolveToTheSameIdsAndUnknownLinksAreIgnored()
    {
        var snapshot = SnapshotReader.Read(Request(articles: [Article(tags:
            ["[[后端]]", "[[Tags/后端]]", "[[Tags/后端|显示名称]]", "[[Tags/后端.md]]", "[[不存在]]", "[[SubTags/CSharp]]", "plain text"],
            subTags: ["[[CSharp|CSharp]]", "[[SubTags/CSharp]]", "[[Tags/后端]]"])]), Settings);
        Assert.Equal([TagId], snapshot.Articles.Single().TagIds);
        Assert.Equal([SubTagId], snapshot.Articles.Single().SubTagIds);
        Assert.Equal(4, snapshot.IgnoredReferences);
    }

    [Fact]
    public void DraftsAreIgnoredBeforeTheirMetadataIsValidated()
    {
        var snapshot = SnapshotReader.Read(Request(articles: [new ArticleInput { Published = false }, Article()]), Settings);
        Assert.Single(snapshot.Articles);
    }

    [Fact]
    public void AllMissingReferencesProduceAnUnclassifiedArticle()
    {
        var snapshot = SnapshotReader.Read(Request(articles: [Article(tags: ["[[missing]]"], subTags: [null])]), Settings);
        Assert.Empty(snapshot.Articles[0].TagIds);
        Assert.Empty(snapshot.Articles[0].SubTagIds);
        Assert.Equal(2, snapshot.IgnoredReferences);
    }

    [Fact]
    public void EmptyCompleteSnapshotIsValidButIncompleteSnapshotIsNot()
    {
        Assert.Empty(SnapshotReader.Read(Request(articles: [], tags: [], subTags: []), Settings).Articles);
        Assert.Throws<SyncValidationException>(() => SnapshotReader.Read(Request(complete: false), Settings));
        Assert.Throws<SyncValidationException>(() => SnapshotReader.Read(new SyncRequest { Complete = true }, Settings));
    }

    [Theory]
    [InlineData("../secret.md")]
    [InlineData("/root/note.md")]
    [InlineData("C:/secret.md")]
    [InlineData(".git/config.md")]
    [InlineData("folder\\note.md")]
    [InlineData("Tags/article.md")]
    public void UnsafeArticlePathsAreRejected(string path) => Assert.Throws<SyncValidationException>(
        () => SnapshotReader.Read(Request(articles: [Article(sourcePath: path)]), Settings));

    [Fact]
    public void DuplicateIdsAndSlugsAreRejected()
    {
        Assert.Throws<SyncValidationException>(() => SnapshotReader.Read(Request(articles: [Article(id: TagId)]), Settings));
        Assert.Throws<SyncValidationException>(() => SnapshotReader.Read(Request(articles:
            [Article(), Article(id: "44444444444444448444444444444444", sourcePath: "other.md")]), Settings));
    }

    [Fact]
    public void HashIncludesMetadataAndRelationshipsButNotReferenceSyntaxOrOrdering()
    {
        var original = SnapshotReader.Read(Request(), Settings).Articles[0];
        var aliases = SnapshotReader.Read(Request(articles: [Article(tags: ["[[Tags/后端|别名]]", "[[后端]]"])]), Settings).Articles[0];
        Assert.Equal(original.ContentHash, aliases.ContentHash);
        foreach (var changed in new[]
        {
            original with { Content = "changed" }, original with { Title = "new title" },
            original with { Slug = "new-slug" }, original with { SourcePath = "moved.md" },
            original with { Description = "description" }, original with { Recommend = true },
            original with { LastEditedTime = original.LastEditedTime.AddSeconds(1) },
            original with { TagIds = [] }, original with { SubTagIds = [] }
        }) Assert.NotEqual(original.ContentHash, SnapshotReader.Hash(changed));
        Assert.Equal(SnapshotReader.Hash(original with { TagIds = [TagId, SubTagId] }),
            SnapshotReader.Hash(original with { TagIds = [SubTagId, TagId] }));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("Basic invalid", false)]
    [InlineData("Bearer wrong", false)]
    [InlineData("Bearer ", false)]
    [InlineData("valid", true)]
    public void OnlyTheConfiguredBearerTokenIsAccepted(string? header, bool accepted)
    {
        var context = new DefaultHttpContext();
        if (header is not null) context.Request.Headers.Authorization = header == "valid" ? "Bearer " + Settings.Token : header;
        var filterContext = new AuthorizationFilterContext(new ActionContext(context, new RouteData(), new ActionDescriptor()), []);
        new SyncTokenFilter(Options.Create(Settings)).OnAuthorization(filterContext);
        Assert.Equal(accepted, filterContext.Result is null);
    }

    [Theory]
    [InlineData(200, "{\"revalidated\":true}", true)]
    [InlineData(200, "{\"revalidated\":false}", false)]
    [InlineData(200, "null", false)]
    [InlineData(200, "[]", false)]
    [InlineData(200, "<html>login</html>", false)]
    [InlineData(302, "", false)]
    [InlineData(503, "", false)]
    public async Task CacheRefreshRequiresAnAcknowledgement(int status, string body, bool succeeds)
    {
        var handler = new CallbackHandler((request, _) =>
        {
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal(Settings.Token, request.Headers.Authorization?.Parameter);
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(body) });
        });
        var client = new RevalidationClient(new HttpClient(handler), Options.Create(new RevalidationOptions
            { Url = "http://localhost:3000/api/revalidate", Token = Settings.Token }));
        if (succeeds) await client.RefreshAsync(new string('a', 40), default);
        else await Assert.ThrowsAsync<CacheRefreshException>(() => client.RefreshAsync(new string('a', 40), default));
    }
}

internal sealed class CallbackHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => callback(request, cancellationToken);
}
