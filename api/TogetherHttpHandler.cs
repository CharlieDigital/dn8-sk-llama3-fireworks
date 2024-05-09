/// <summary>
/// The Together stream API response does not match the OpenAI spec.  So this
/// custom delegating handler rewrites the response.
/// </summary>
/// <remarks>
/// Workaround for: https://github.com/Azure/azure-sdk-for-net/issues/43952 This
/// is not a full fix since this is a buffered solution.
/// </remarks>
public class TogetherHttpHandler : DelegatingHandler {
  public TogetherHttpHandler() {
    InnerHandler = new HttpClientHandler();
  }

  protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken
  ) {
    var response = await base.SendAsync(request, cancellationToken);

    var body = await response.Content.ReadAsStringAsync();

    // This causes an error in: StreamingChatCompletionsUpdate.Serialization
    // See: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/src/Custom/ChatCompletions/StreamingChatCompletionsUpdate.Serialization.cs#L239
    body = body.Replace(",\"tool_calls\":null", "");

    response.Content = new StringContent(body);

    // Read the incoming JSON and transform into the target JSON.
    return response;
  }
}
