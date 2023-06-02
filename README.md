# CaptainA - A mobile app for practising Finnish pronunciation

This repo contain the code for the mobile app, but also for the back end [server](https://github.com/Usin2705/CaptainA_unity/tree/main/Server). \
The app can be downloaded at [Google Play](https://play.google.com/store/apps/details?id=com.Kielibuusti.CaptainA) or [Apple App Store](https://apps.apple.com/ph/app/captaina/id6444659467) \
The current version of the wav2vec 2.0 model used in the app can be downloaded in [HuggingFace](https://huggingface.co/Usin2705/CaptainA_v0) \
The demo paper of CaptainA app can be read [here](https://aclanthology.org/2023.nodalida-1.26) \
And the detailed analysis and documentation on the development of the CaptainA app can be read in the [Master's thesis](http://urn.fi/URN:NBN:fi:aalto-202305213302)\

# How to run the server
## Setup
The [server](https://github.com/Usin2705/CaptainA_unity/tree/main/Server) is running with Nginx. \
You can download the model in [HuggingFace](https://huggingface.co/Usin2705/CaptainA_v0). Copy that model to the folder "nhan_wav2vec2-xls-r-300m-finnish-ent-10" \
You need to open a port for CaptainA. Let's call it PORT. You should update the new port number in the [Docker file](https://github.com/Usin2705/CaptainA_unity/blob/main/Server/Dockerfile) \
The server comes with a Dockerfile, so you can run it without any extra installations (aside from Nginx and Podman/Docker) \
You can use either Docker or Podman for the Dockerfile. The default command is for Podman, but you can replace it with Docker by simply changing the command from podman to docker. \

First, build the image from the docker file
```
podman build --pull --rm -f "Dockerfile" -t captaina:latest "." 
```
To make sure the server will automatically restart even if the backend reboot, use the following command (choose between docker or podman):
```
docker run --restart=unless-stopped -d -p PORT:PORT --name captaina_server captaina
podman run --restart=always -d -p PORT:PORT --name captaina_server captaina
```
Then reboot the server to check if the docker container automatically restart.

## More details

You can set up more workers or threads in the Dockerfile. The default is 2 workers with 1 threads, but it's because the server we use only have 4 Intel X5670 @ 2.93GHz
```
CMD exec gunicorn --bind :$PORT main:app --workers 2 --threads 1 --access-logfile /app/logs/gunicorn-access.log --error-logfile /app/logs/gunicorn-error.log --capture-output --log-level debug
```
The CaptainA server expect a Rest API POST with the form: key "file": wav file, key "transcript": the target text (that users are expected to read) 

The CaptainA server response is a JSON with the following format:
```
{
  "levenshtein": OPS List (see example below),
  "prediction": string prediction of ASR model,
  "score": list of pronunciation score for each phoneme: [0.10, 0.35, 0.66, 0.01]
  "warning": list of warning (int): [0, 1, 2, 3] There are currently 4 warnings: word too short, NP should be pronounced as MP, NK and NG sound, Boundary gemination - Mene pois!
}
```
Example of ops list, the list was just the result from ```Levenshtein.editops(normal_trans, prediction)``` converted into a dictionary for easier to handle in Unity.
```
[
  {"ops": "insert", "tran_index": 0, "pred_index": 2},
  {"ops": "delete", "tran_index": 3, "pred_index": 4},
  {"ops": "replace", "tran_index": 5, "pred_index": 6}
]
```

# License

The CaptainA is licensed under the GNU Affero General Public License, version 3 or later. The license can be found [here](https://github.com/Usin2705/CaptainA_unity/blob/main/LICENSE).

Other related work to CaptainA made by the authors (thesis work, journal articles, audio samples, pictures, videos ...) are licensed under a Creative Commons "Attribution-NonCommercial-ShareAlike 4.0 International" (BY-NC-SA 4.0) [license](https://creativecommons.org/licenses/by-nc-sa/4.0/). 

Other works not made by the authors are licensed accordingly to their respective owners:
- The authors of Oma Suomi 1: Kristiina Kuparinen, Terhi Tapaninen and Finn Lectura have give us permission to use the text in Oma Suomi 1 to create the flashcard for the CaptainA app.
- Anki is licensed under AGPL3.
- SuperMemo2 is open to the public: Algorithm SM-2, (C) Copyright SuperMemo World, 1991. https://www.supermemo.com.
- The side picture illustrations is created by Aino Huhtaniemi (https://ainohuhtaniemi.com/), and she gave her permission to use and modify her original illustrations for the CaptainA app.
- Icons used in the application are from Google under Apache License 2.0.
- Photo illustrations and some of the videos were made with the contribution of Aija Elg and Noora Heikiö from Aalto University Language Centre.
- Some audio samples are from Aalto University Language Centre.
- Some audio samples are from Common Voice 11.0, licensed under [Creative Commons Zero 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
- Some audio samples and text examples are from LibriVox under Public Domain.

