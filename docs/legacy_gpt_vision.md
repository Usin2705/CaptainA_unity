# Legacy GPT vision grading (`GPTRatingVision`)

This documents a coroutine removed from `Assets/Scripts/Managers/NetworkManager.cs`, so
that the reason it went is recorded and so it can be restored deliberately rather than
rediscovered by accident.

The newer speech assessment path is the DTA server
([aalto-speech/dta-server](https://github.com/aalto-speech/dta-server)); see
[`TO_FRONTEND.md`](TO_FRONTEND.md). The older pronunciation server is documented in
[`legacy_pronunciation_server.md`](legacy_pronunciation_server.md).

## What it did

`GPTRatingVision(GameObject scoreButtonGO, string transcript, bool isFinnish = true)`
graded the "describe the picture" task by sending **both** the user's transcript and the
picture itself to a vision model, so the model could compare what the user said against
what was actually in the image.

Roughly:

1. Load `describeImage.png` from `Application.persistentDataPath`, falling back to
   `Resources/GenAI/describeImage` and saving a copy if the file was missing.
2. Base64-encode it and inline it as a `data:image/jpeg;base64,...` image URL.
3. Build a long grading prompt inline in the method — the only place that prompt existed.
   It asked for five scored sections (Corrected/Suggested Description, Accuracy of
   Description, Vocabulary, Pronunciation, Grammar, Overall), each 1-5, with the final
   rating as the last three characters of the response.
4. POST to Aalto's Azure gateway at
   `aalto-openai-apigw.azure-api.net/v1/openai/gpt4-vision-preview/chat/completions`,
   authenticating with `Ocp-Apim-Subscription-Key`.
5. Put the last three characters of the reply on the score button and store the full
   response in `chatGPTGrading`.

## Why it was removed

- **The model is retired.** It targeted `gpt-4-vision-preview`, which no longer exists.
  The request could not succeed as written.
- **It had no callers.** The method was `private` and nothing in the project invoked it.
  The live describe-task path is `GPTTranscribeWhisper` → `GPTRatingText`, which handles
  images for task types `C` and `C2` inline using `gpt-4o` and OpenAI directly.
- It was 128 lines, roughly a tenth of `NetworkManager.cs`, and made the file harder to
  read for no working behaviour.

## Restoring it

The code is in git history. The last commit containing it is `e6a4922`
("feat: add option to access ASA func from code"):

```bash
git show e6a4922:Assets/Scripts/Managers/NetworkManager.cs > /tmp/old_NetworkManager.cs
# GPTRatingVision was lines 957-1084 of that file
```

If you do restore it, note:

- The endpoint and model both need replacing. The equivalent today is `gpt-4o` (or newer)
  via OpenAI directly, which `GPTRatingText` already does — the vision-specific coroutine
  is probably not worth reviving as a separate path.
- The grading prompt was embedded in the method rather than in `TextUtils` or `Secret`,
  unlike every other prompt in the project. It is only recoverable from that commit.

## Secret fields it depended on

`Secret.cs` is gitignored, so these field names are recorded here and in
[`Secret.cs.template`](../Assets/Scripts/Utils/Secret.cs.template). Values are not in the
repository — ask a maintainer.

| Field | Used for |
| --- | --- |
| `AZUREGPT_API` | `Ocp-Apim-Subscription-Key` header on the Aalto Azure gateway |
| `AALTO_GPT4O_URL` | The gateway's GPT-4o endpoint, used by the commented-out Azure branch in `GPTRatingText` |

Both fields are still declared in `Secret.cs.template`, but **neither is read by any live
code any more**. `NetworkManager` used to assign `AZUREGPT_API` to a `gptAzureToken`
field; that field was removed along with this coroutine, since its only remaining use was
a commented-out line in `GPTRatingText`. `AALTO_GPT4O_URL` likewise appears only in a
comment.

They are kept in the template so that code restored from `e6a4922` still compiles, and so
the names are not lost. If you are certain the Azure path will never come back, they can
be dropped from both the template and `Secret.cs` with no other changes.
