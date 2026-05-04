# Legacy CaptainA Pronunciation Server and API Notes

This document preserves the older CaptainA pronunciation server setup and API notes that were previously kept in the main README. The newer ASA server is maintained separately at [aalto-speech/dta-server](https://github.com/aalto-speech/dta-server).

## Server Installation and Setup

The legacy server in [`MDD_Server/`](../MDD_Server/) runs on Nginx and comes with a Dockerfile, allowing it to run without any extra installations aside from Nginx and Docker/Podman. Here are the steps to set up and run the server:

1. **Model Download**: Download the model from [HuggingFace](https://huggingface.co/Usin2705/CaptainA_v0) and copy it to the folder `PATH_TO_SERVER_FOLDER/models/nhan_wav2vec2-xls-r-300m-finnish-ent-10`.

2. **Port Setup**: Open a port for CaptainA, referred to here as `PORT`, and update the new port number in the Docker file.

3. **Docker/Podman Setup**: You can use either Docker or Podman for the [`MDD_Server/Dockerfile`](../MDD_Server/Dockerfile). The default command is for Podman, but you can replace it with Docker by changing the command from `podman` to `docker`.

4. **Build the Image**: First, build the image from the Dockerfile:

```bash
podman build --pull --rm -f "Dockerfile" -t captaina:latest "."
```

5. **Run the Server**: To ensure the server automatically restarts even if the backend reboots, use one of the following commands.

For Docker:

```bash
docker run --restart=unless-stopped -d -p PORT:PORT --name captaina_server captaina
```

For Podman:

```bash
podman run --restart=always -d -p PORT:PORT --name captaina_server captaina
```

6. **Reboot the Server**: Reboot the server to check if the Docker container automatically restarts.

You can set up more workers or threads in the Dockerfile. The default is 2 workers with 1 thread, because the original server had 4 Intel X5670 @ 2.93GHz CPUs.

## API Usage

The legacy CaptainA server expects a REST API POST with the following keys:

- **file**: wav file
- **transcript**: the target text that users are expected to read

The server responds with JSON in the following format:

```json
{
  "levenshtein": [OPS List],
  "prediction": "mustikka",
  "score": [0.10, 0.75, 0.88, 0.90, 0.99, 0.95, 0.66, 0.01],
  "warning": [0, 1, 2, 3]
}
```

Where:

- **levenshtein**: OPS list; see the example below.
- **prediction**: string prediction from the ASR model.
- **score**: list of pronunciation scores for each phoneme. For `mustikka`, this would be `[0.10, 0.75, 0.88, 0.90, 0.99, 0.95, 0.66, 0.01]`, indicating the first and last letter/phone, `m` and `a`, were mispronounced.
- **warning**: list of warning IDs. There are currently 4 warnings: word too short, NP should be pronounced as MP, NK and NG sound, and boundary gemination, as in `Mene pois!`.

Random example of an ops list, converted from `Levenshtein.editops(transcript, prediction)` into a dictionary format for straightforward usage in Unity:

```json
[
  {"ops": "insert", "tran_index": 0, "pred_index": 2},
  {"ops": "delete", "tran_index": 3, "pred_index": 4},
  {"ops": "replace", "tran_index": 5, "pred_index": 6}
]
```
