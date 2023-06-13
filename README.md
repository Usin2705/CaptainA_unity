# CaptainA - A Mobile App for Practicing Finnish Pronunciation

Welcome to the repository of CaptainA, a mobile application designed to help users practice their Finnish pronunciation. This repository contains the code for both the mobile application and the backend server.

## About the Project

CaptainA is a mobile application that utilizes the wav2vec 2.0 model for Finnish pronunciation practice. The app is available for download on both [Google Play](https://play.google.com/store/apps/details?id=com.Kielibuusti.CaptainA) and the [Apple App Store](https://apps.apple.com/ph/app/captaina/id6444659467). The current version of the wav2vec 2.0 model used in the app can be downloaded from HuggingFace. The [demo paper](https://aclanthology.org/2023.nodalida-1.26) offers a short introduction. For more detailed analysis and documentation on the development of the CaptainA app, you can refer to [Master's thesis](http://urn.fi/URN:NBN:fi:aalto-202305213302).


## Server Installation and Setup

The [server](https://github.com/Usin2705/CaptainA_unity/tree/main/Server) for CaptainA runs on Nginx and comes with a Dockerfile, allowing it to run without any extra installations (aside from Nginx and Docker/Podman). Here are the steps to set up and run the server:

1. **Model Download**: Download the model from [HuggingFace](https://huggingface.co/Usin2705/CaptainA_v0) and copy it to the folder "PATH_TO_SERVER_FOLDER/models/nhan_wav2vec2-xls-r-300m-finnish-ent-10".

2. **Port Setup**: Open a port for CaptainA (let's call it PORT) and update the new port number in the Docker file.

3. **Docker/Podman Setup**: You can use either Docker or Podman for the [Dockerfile](https://github.com/Usin2705/CaptainA_unity/blob/main/Server/Dockerfile). The default command is for Podman, but you can replace it with Docker by simply changing the command from `podman` to `docker`.

4. **Build the Image**: First, build the image from the docker file using the command: `podman build --pull --rm -f "Dockerfile" -t captaina:latest "."`

5. **Run the Server**: To ensure the server will automatically restart even if the backend reboots, use the following command (choose between docker or podman):

   For Docker: `docker run --restart=unless-stopped -d -p PORT:PORT --name captaina_server captaina`

   For Podman: `podman run --restart=always -d -p PORT:PORT --name captaina_server captaina`

6. **Reboot the Server**: Reboot the server to check if the docker container automatically restarts.

You can set up more workers or threads in the Dockerfile. The default is 2 workers with 1 thread, but this is because the server we use only has 4 Intel X5670 @ 2.93GHz.

## API Usage

The CaptainA server expects a Rest API POST with the following keys:

- **file**: wav file
- **transcript**: the target text (that users are expected to read)

The server responds with a JSON in the following format:

```json
{
 "levenshtein": [OPS List],
 "prediction": "mustikka",
 "score": [0.10, 0.75, 0.88, 0.90, 0.99, 0.95, 0.66, 0.01],
 "warning": [0, 1, 2, 3]
}
```

Where:

- **levenshtein**: OPS List (see example below)
- **prediction**: string prediction of ASR model
- **score**: list of pronunciation scores for each phoneme (for "mustikka" it would be [0.10, 0.75, 0.88, 0.90, 0.99, 0.95, 0.66, 0.01], indicating the first and last letter/phone (m and a) were mispronounced)
- **warning**: list of warning (int): [0, 1, 2, 3]. There are currently 4 warnings: word too short, NP should be pronounced as MP, NK and NG sound, Boundary gemination - Mene pois!

Random example of ops list, the list was just the result from `Levenshtein.editops(transcript, prediction)` converted into a dictionary for straightforward usage in Unity.

```json
[
  {"ops": "insert", "tran_index": 0, "pred_index": 2},
  {"ops": "delete", "tran_index": 3, "pred_index": 4},
  {"ops": "replace", "tran_index": 5, "pred_index": 6}
]
```


## License

The CaptainA is licensed under the [GNU Affero General Public License, version 3 or later](https://github.com/Usin2705/CaptainA_unity/blob/main/LICENSE). Other related work to CaptainA made by the authors (thesis work, journal articles, audio samples, pictures, videos ...) are licensed under a Creative Commons "Attribution-NonCommercial-ShareAlike 4.0 International" (BY-NC-SA 4.0) [license](https://creativecommons.org/licenses/by-nc-sa/4.0/).

Other works not made by the authors are licensed accordingly to their respective owners:

- The authors of Oma Suomi 1: Kristiina Kuparinen, Terhi Tapaninen and Finn Lectura have given us permission to use the text in Oma Suomi 1 to create the flashcard for the CaptainA app.
- Anki is licensed under AGPL3.
- SuperMemo2 is open to the public: Algorithm SM-2, (C) Copyright SuperMemo World, 1991. [https://www.supermemo.com](https://www.supermemo.com).
- The side picture illustrations are created by Aino Huhtaniemi ([https://ainohuhtaniemi.com/](https://ainohuhtaniemi.com/)), and she gave her permission to use and modify her original illustrations for the CaptainA app.
- Icons used in the application are from Google under Apache License 2.0.
- Photo illustrations and some of the videos were made with the contribution of Aija Elg and Noora Heikiö from Aalto University Language Centre.
- Some audio samples are from Aalto University Language Centre.
- Some audio samples are from Common Voice 11.0, licensed under [Creative Commons Zero 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
- Some audio samples and text examples are from LibriVox under Public Domain.
