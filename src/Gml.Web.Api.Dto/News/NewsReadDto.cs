using System;
using GmlCore.Interfaces.Enums;

namespace Gml.Web.Api.Dto.News;

public record NewsReadDto
{
    public string Title { get; }
    public string Content { get; }
    public DateTimeOffset Date { get; }
    NewsListenerType Type { get; set; }
}
