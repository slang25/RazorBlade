﻿using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RazorBlade.Support;

namespace RazorBlade;

/// <summary>
/// Base class for Razor templates.
/// </summary>
public abstract class RazorTemplate : IEncodedContent
{
    /// <summary>
    /// The <see cref="TextWriter"/> which receives the output.
    /// </summary>
    protected internal TextWriter Output { get; internal set; } = new StreamWriter(Stream.Null);

    /// <summary>
    /// The cancellation token.
    /// </summary>
    protected internal CancellationToken CancellationToken { get; private set; }

    /// <summary>
    /// Renders the template synchronously and returns the result as a string.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// Use this only if the template does not use <c>@async</c> directives.
    /// </remarks>
    [ConditionalOnAsync(false, Message = $"The generated template is async. Use {nameof(RenderAsync)} instead.")]
    public string Render(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var renderTask = RenderAsync(cancellationToken);
        if (renderTask.IsCompleted)
            return renderTask.Result;

        return Task.Run(async () => await renderTask.ConfigureAwait(false), CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Renders the template synchronously to the given <see cref="TextWriter"/>.
    /// </summary>
    /// <param name="textWriter">The <see cref="TextWriter"/> to write to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// Use this only if the template does not use <c>@async</c> directives.
    /// </remarks>
    [ConditionalOnAsync(false, Message = $"The generated template is async. Use {nameof(RenderAsync)} instead.")]
    public void Render(TextWriter textWriter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var renderTask = RenderAsync(textWriter, cancellationToken);
        if (renderTask.IsCompleted)
        {
            renderTask.GetAwaiter().GetResult();
            return;
        }

        Task.Run(async () => await renderTask.ConfigureAwait(false), CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Renders the template asynchronously and returns the result as a string.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// Use this if the template uses <c>@async</c> directives.
    /// </remarks>
    public async Task<string> RenderAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var output = new StringWriter();
        await RenderAsync(output, cancellationToken).ConfigureAwait(false);
        return output.ToString();
    }

    /// <summary>
    /// Renders the template asynchronously to the given <see cref="TextWriter"/>.
    /// </summary>
    /// <param name="textWriter">The <see cref="TextWriter"/> to write to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// Use this if the template uses <c>@async</c> directives.
    /// </remarks>
    public async Task RenderAsync(TextWriter textWriter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var previousState = (Output, CancellationToken);

        try
        {
            Output = textWriter;
            CancellationToken = cancellationToken;

            await ExecuteAsync().ConfigureAwait(false);
        }
        finally
        {
            (Output, CancellationToken) = previousState;
        }
    }

    /// <summary>
    /// Executes the template and appends the result to <see cref="Output"/>.
    /// </summary>
    [PublicAPI]
    protected internal virtual Task ExecuteAsync()
        => Task.CompletedTask; // The IDE complains when this method is abstract :(

    /// <summary>
    /// Writes a literal value to the output.
    /// </summary>
    /// <param name="value">The value to write.</param>
    [PublicAPI]
    protected internal void WriteLiteral(string? value)
        => Output.Write(value);

    /// <summary>
    /// Write a value to the output.
    /// </summary>
    /// <param name="value">The value to write.</param>
    [PublicAPI]
    protected internal abstract void Write(object? value);

    /// <summary>
    /// Write already encoded content to the output.
    /// </summary>
    /// <param name="content">The template to render.</param>
    protected internal void Write(IEncodedContent? content)
        => content?.WriteTo(Output);

    /// <summary>
    /// Begins writing an attribute.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="prefix">The attribute prefix, which is the text from the whitespace preceding the attribute name to the quote before the attribute value.</param>
    /// <param name="prefixOffset">The prefix offset in the Razor file.</param>
    /// <param name="suffix">The suffix, consisting of the end quote.</param>
    /// <param name="suffixOffset">The suffix offset in the Razor file.</param>
    /// <param name="attributeValuesCount">The count of attribute value parts, which is the count of subsequent <see cref="WriteAttributeValue"/> calls.</param>
    [PublicAPI]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal abstract void BeginWriteAttribute(string name, string prefix, int prefixOffset, string suffix, int suffixOffset, int attributeValuesCount);

    /// <summary>
    /// Writes part of an attribute value.
    /// </summary>
    /// <param name="prefix">The value prefix, consisting of the whitespace preceding the value.</param>
    /// <param name="prefixOffset">The prefix offset in the Razor file.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="valueOffset">The value offset in the Razor file.</param>
    /// <param name="valueLength">The value length in the Razor file.</param>
    /// <param name="isLiteral">Whether the value is a literal.</param>
    [PublicAPI]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal abstract void WriteAttributeValue(string prefix, int prefixOffset, object? value, int valueOffset, int valueLength, bool isLiteral);

    /// <summary>
    /// Ends writing an attribute.
    /// </summary>
    [PublicAPI]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal abstract void EndWriteAttribute();

    void IEncodedContent.WriteTo(TextWriter textWriter)
        => Render(textWriter, CancellationToken.None);
}
