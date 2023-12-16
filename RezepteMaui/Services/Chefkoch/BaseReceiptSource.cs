namespace Rezepte.Services.Chefkoch
{
    public abstract class BaseReceiptSource: IReceiptSource
    {

        public BaseReceiptSource() { }

        public abstract Task<ISourceReceipt> FromUriAsync(string uri);
        public abstract Task<string[]> ExtractUris(string html);
        protected async Task<string> DownloadFileAsync(string uri)
        {
            var tempFilePath = Path.GetTempFileName();
            using (HttpClient client = new HttpClient())
                using (var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead)
                                                  .ConfigureAwait(false))
                    try
                    {
                        if (!response.IsSuccessStatusCode)
                            throw new ApplicationException($"Error in download: {response.StatusCode}");

                        var total = response.Content.Headers.ContentLength ?? -1L;
                        double progress = 0;

                        using (var streamToReadFrom = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            var totalRead = 0L;
                            var buffer = new byte[2048];
                            var isMoreToRead = true;
                            var fileWriteTo = tempFilePath;
                            var output = new FileStream(fileWriteTo, FileMode.Create);
                            do
                            {
                                var read = await streamToReadFrom.ReadAsync(buffer,
                                                                            0,
                                                                            buffer.Length,
                                                                            CancellationToken.None);

                                if (read == 0)
                                    isMoreToRead = false;
                                else
                                {
                                    await output.WriteAsync(buffer, 0, read);

                                    totalRead += read;

                                    progress = ((totalRead * 1d) / (total * 1d)) * 100;
                                }
                            }
                            while (isMoreToRead);

                            output.Close();
                        }
                    }
                    catch
                    {
                        if (File.Exists(tempFilePath))
                            File.Delete(tempFilePath);
                        throw;
                    }
            return tempFilePath;
        }

        protected string GetTagAttribute(string tag, string valueNames)
        {
            var valueNameParts = valueNames.Split('|');
            foreach (var valueName in valueNameParts)
            {
                int offset = tag.IndexOf($"{valueName}=");
                if (offset < 0)
                    continue;
                tag = tag.Remove(0, offset + valueName.Length + 1);

                offset = tag.IndexOfAny(new char[] { ' ', '>' });
                if (offset < 0)
                    return string.Empty;
                tag = tag.Remove(offset);
                return tag.TrimEnd('/').Trim('"');
            }
            return string.Empty;
        }
        protected string FindTagValue(string content, params string[] tags)
        {
            content = FindTag(content, true, tags);
            int offset = content.IndexOf($"</{tags.Last()}");
            if (offset >= 0)
                content = content.Remove(offset);
            return content.Replace("<br>", "\r\n").Trim();
        }

        protected string FindTag(string content, bool removeTag, params string[] tags)
        {
            for (int idx = 0; idx < tags.Length; idx++)
            {
                var tag = tags[idx];
                var tagParts = tag.Split('|');
                var tagName = tagParts.First();
                var isLast = idx == (tags.Length - 1);
                while (!string.IsNullOrWhiteSpace(content))
                {
                    int offset = content.IndexOf($"<{tagName}");
                    if (offset < 0)
                        return string.Empty;
                    content = content.Remove(0, offset);
                    var tagStart = content.Remove(content.IndexOf(">") + 1);
                    if (tagStart != $"<{tagName}>" && !tagStart.StartsWith($"<{tagName} "))
                    {
                        content = content.Remove(0, tagStart.Length);
                        continue;
                    }

                    if (tagParts.Length > 1)
                    {
                        var tagFound = !tagParts.Skip(1)
                                                .Any(t =>
                                                {
                                                    // Die erste Eigenschaft suchen, die nicht passt:
                                                    var args = t.Split('=');
                                                    int offset = tagStart.IndexOf($"{args[0]}=\"");
                                                    if (offset < 0)
                                                        return true;
                                                    var param = tagStart.Remove(0, offset + args[0].Length + 2);
                                                    param = param.Remove(param.IndexOf('\"'));
                                                    return param != args[1];
                                                });
                        if (!tagFound)
                        {
                            content = content.Remove(0, tagStart.Length);
                            continue;
                        }
                    }

                    if (tagStart.EndsWith("/>") || !content.Contains($"</{tagName}"))
                        return GetTagAttribute(tagStart, "value|content|src");
                    if (!isLast || removeTag)
                        content = content.Remove(0, tagStart.Length);
                    break;
                }
            }
            return content;
        }
        protected string[] CollectTags(string content, string tagName)
        {
            var startTag = $"<{tagName}>";
            var startTag2 = $"<{tagName} ";
            var endTag = $"</{tagName}>";
            List<string> tags = new List<string>();
            while (content.Length > 0)
            {
                var tag = FindTag(content, false, tagName);
                if (string.IsNullOrWhiteSpace(tag))
                    break;
                int level = 0;
                var newTag = string.Empty;
                do
                {                    
                    int offsetStart = tag.IndexOf(startTag);
                    int offsetEnd = tag.IndexOf(endTag);
                    while (offsetEnd > offsetStart && (offsetStart >= 0))
                    {
                        level += 1;
                        var part = tag.Substring(0, offsetStart + startTag.Length);
                        newTag += part;
                        tag = tag.Remove(0, part.Length);

                        offsetStart = tag.IndexOf(startTag);
                        offsetEnd = tag.IndexOf(endTag);
                    }
                    var part2 = tag.Substring(0, offsetEnd + endTag.Length);
                    newTag += part2;
                    tag = tag.Remove(0, part2.Length);
                    level -= 1;
                }
                while (level > 0);
                tags.Add(newTag);

                content = content.Remove(0, content.IndexOf(newTag));
                content = content.Remove(0, newTag.Length);
            }
            return tags.ToArray();
        }

        
    }
}
