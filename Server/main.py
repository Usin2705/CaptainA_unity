import mimetypes
from transformers import Wav2Vec2Processor, Wav2Vec2ForCTC, AutoFeatureExtractor
from flask import Flask, jsonify, request, Response
import torch
import torchaudio
import os
import logging
import Levenshtein

import utils.myutils as myutils

app = Flask(__name__)

print("App start!")

path_model = "./models/nhan_wav2vec2-xls-r-300m-finnish-ft-ent"
path_data = "./data/"
SAMPLING_RATE = 16000
FILE_NAME = "pronc_huomenna"

MODEL_PATH = {1:"./models/nhan_wav2vec2-xls-r-300m-finnish-ft-ent",
              2:"./models/nhan_wav2vec2-xls-r-300m-finnish-ent",
}

processor = Wav2Vec2Processor.from_pretrained(path_model)
model = Wav2Vec2ForCTC.from_pretrained(path_model)

@app.route("/test_post", methods=['POST'])
def test_post():
    print("test connected")
    return {"prediction":"success", "score":[0.5, 0.9, 0.2, 0.6, 0.8, 0.7, 0.1]}


@app.route("/test")
def test_func():
    
    return "Success"

@app.route("/asr_unity", methods=['POST'])
def pronunc_eval_unity():
#    review = request.args.get('review')
#    api_key = request.args.get('api_key')
#    if review is None or api_key != "MyCustomerApiKey":
#        return jsonify(code=403, message="bad request")
    file = request.files['file']    
    # file.save(os.path.join("/tmp/", FILE_NAME + ".wav"))
    # print(f'file info: {file}')

    transcript = request.form['transcript'].lower()

    path_model_id = int(request.form.get("model_code", 1))
    path_model = MODEL_PATH[path_model_id]

    # We only load model if user switch model, otherwise we use the old model
    # to save processing time
    # (But this would mean bigger memory issue)
    # TODO Not thread safe to have global variable atm

    # processor = Wav2Vec2Processor.from_pretrained(path_model)
    # model = Wav2Vec2ForCTC.from_pretrained(path_model)
    
    transcript = myutils.textSanitize(transcript)
    #logging.warning(f"Transcript {transcript.upper()}")
    #logging.warning(transcript)    

    vocab = processor.tokenizer.get_vocab()    

    with torch.inference_mode():
        #waveform, sr = torchaudio.load("/tmp/" + FILE_NAME + ".wav")
        waveform, sr = torchaudio.load(file)
        waveform = torchaudio.transforms.Resample(sr, SAMPLING_RATE)(waveform)  

    # transpose to match dimension with Wav2Vec2 processor
    input_audio = torch.transpose(waveform, 1,0)

    # indexing to get only 1 channel (incase the recording are from 2 channels)
    input_audio = input_audio[:, 0]                       

    input_values = processor(input_audio, sampling_rate=SAMPLING_RATE, return_tensors="pt").input_values

    with torch.no_grad():
        logits = model(input_values).logits

    predicted_ids = torch.argmax(logits, dim=-1)
    prediction = processor.batch_decode(predicted_ids)[0]
    tokens = [vocab[c] for c in transcript]     

    log_softmax = torch.log_softmax(logits, dim=-1)
    emission = log_softmax[0].cpu().detach()   

    trellis = myutils.get_trellis(emission, tokens, blank_id=model.config.pad_token_id)     
    path = myutils.backtrack(trellis, emission, tokens, blank_id=model.config.pad_token_id)    
    segments = myutils.merge_repeats(transcript, path)       

    fa_score = []

    for seg in segments:
        fa_score.append(seg.score)

    #logging.warning(f"Prediction {prediction}, Score {fa_score}")

    normal_trans = transcript.replace('|', ' ')
    listWarning = myutils.warning_detect(normal_trans, prediction)
    ops = Levenshtein.editops(normal_trans, prediction)

    print(f'Using model: {path_model}')
    print(f'Transcript: {normal_trans}')
    print(f'Prediction: {prediction}')
    print(f'Score: {fa_score}')
    print(f'Warning: {listWarning}')
    print(f'OPS: {ops}')

    # Convert back to easier list for Unity
    ops_list = []
    for item in ops:
        ops_list.append({"ops":item[0], "tran_index":item[1], "pred_index":item[2]})

    return {"levenshtein":ops_list, "prediction":prediction, "score":fa_score, "warning": listWarning}    


@app.route("/asr", methods=['POST'])
def pronunc_eval():
#    review = request.args.get('review')
#    api_key = request.args.get('api_key')
#    if review is None or api_key != "MyCustomerApiKey":
#        return jsonify(code=403, message="bad request")
    file = request.files['file']    
    file.save(os.path.join("/tmp/", FILE_NAME + ".3gp"))
    print(file)

    transcript = request.form['transcript'].lower()
    print(transcript)

    path_model_id = int(request.form.get("model_code", 1))
    path_model = MODEL_PATH[path_model_id]

    print(f'Using model: {path_model}')
    
    transcript = myutils.textSanitize(transcript)

    vocab = processor.tokenizer.get_vocab()    

    os.system(f'ffmpeg -y -i /tmp/{FILE_NAME}.3gp /tmp/{FILE_NAME}.wav')

    with torch.inference_mode():
        waveform, sr = torchaudio.load("/tmp/" + 'pronc_huomenna.wav')
        waveform = torchaudio.transforms.Resample(sr, SAMPLING_RATE)(waveform)  

    # transpose to match dimension with Wav2Vec2 processor
    input_audio = torch.transpose(waveform, 1,0)

    # indexing to get only 1 channel (incase the recording are from 2 channels)
    input_audio = input_audio[:, 0]                       

    input_values = processor(input_audio, sampling_rate=SAMPLING_RATE, return_tensors="pt").input_values

    with torch.no_grad():
        logits = model(input_values).logits

    predicted_ids = torch.argmax(logits, dim=-1)
    prediction = processor.batch_decode(predicted_ids)
    tokens = [vocab[c] for c in transcript]     

    log_softmax = torch.log_softmax(logits, dim=-1)
    emission = log_softmax[0].cpu().detach()   

    trellis = myutils.get_trellis(emission, tokens, blank_id=model.config.pad_token_id)     
    path = myutils.backtrack(trellis, emission, tokens, blank_id=model.config.pad_token_id)    
    segments = myutils.merge_repeats(transcript, path)       

    fa_score = []

    for seg in segments:
        fa_score.append(seg.score)

    #logging.warning(f"Prediction {prediction}, Score {fa_score}")

    return {"prediction":prediction[0], "score":fa_score}


if __name__ == '__main__':
    server_port = os.environ.get('PORT', '8080')
    app.run(debug=False, port=server_port, host='0.0.0.0')


#exec gunicorn --bind :5000 main:app --workers 1 --threads 1 --timeout 0
#gunicorn --bind :52705 main:app --workers 4 --threads 1 --timeout 0 --daemon --access-logfile /l/captaina-server/logs/gunicorn-access.log --error-logfile /l/captaina-server/logs/gunicorn-error.log --capture-output --log-level debug

#netstat -aWn --programs | grep 52705
#ldconfig /usr/local/lib64/
#pkill -f gunicorn