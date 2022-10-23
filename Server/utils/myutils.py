from dataclasses import dataclass, field
import torch
import re

WARNING_CODE = {"AAA": 0, "NP": 1, "NGK": 2, "MENEP":3,}

RE_DOUBLE_VOWELS = r'([aeiouyöäå])\1'
RE_QUAD_VOWELS = r'([aeiouyöäå]){4,}'
RE_CONSONANT = r'([dhjklmnpvrst])'

# The rest of this list is noun that ended in e that have boundary gemination
LIST_IMPERATIVE = ["tule", "anna", "mene", "kone", " kirjoita", "vastaa", "sinne", "ostaa", "lähteä", "ota", "oteta", "ottako", 
                   "otettako", "ottane", "otettane", "vastata", "imuroida", "menkö", "voida", "osteta", "mennä", "imuroida", "kiinni",
                   "luo", "taa", "hame", "terve", "askele", "jae", "jakouurre", "jalkine", "jasmike", "jatke", "jatkeaine", "jauhe", "tiede",
                   "ääneti", "alasti", "huoleti", "vaiti", "saati", "alati", "iäti", 
                   'ihanne', 'ihme', 'iljanne', 'ilmanpaine', 'ilme', 'ilotulite', 'ilve', 'imarre', 'imelle', 'immuunivaste', 'imuke', 'isoahde', 
                   'isorinne', 'helle', 'helmivene', 'helve', 'henkikaste', 'heprealaiskirje', 'herkiste', 'herne', 'herrainhuone', 'heräte', 'hete', 
                   'hevoslääketiede', 'hidaste', 'hie', 'hiekkaeste', 'hierre', 'hietarinne', 'hilloke', 'hilse', 'hioke', 'hirttouurre', 'hitaussäde', 
                   'hiue', 'hiuslaite', 'hiuslisäke', 'hiusvoide', 'hiutale', 'hoitoneste', 'home', 'houre', 'huiske', 'huone', 'hurme', 'huume', 'huurre', 
                   'hylje', 'hyve', 'hyödyke', 'häive', 'häme', 'härpäke', 'höyde', 'höyste', 'aaltoliike', 'aaltosulje', 'aapapurje', 'aarnialue', 'aarre', 
                   'aate', 'aate', 'aave', 'adjektiivilauseke', 'ahde', 'ahne', 'aie', 'aihe', 'aikajänne', 'aikavyöhyke', 'aine', 'ajanviete', 
                   'ajatusvirhe', 'alahele', 'alakäsite', 'alivuokrasuhde', 'alkeet', 'alkuaine', 'aloite', 'alue', 'aluke', 'alumiinivene', 
                   'alusrakenne', 'alusvaate', 'amme', 'ane', 'anne', 'anniskelualue', 'ape', 'apuvelvoite', 'armonvälineet', 'ase', 'asenne', 
                   'asete', 'aste', 'astianpesuaine', 'asuinalue', 'asuste', 'aukile', 'autoliike', 'avanne', 'asunto osake', 'biopolttoaine', 
                   'celsiusaste', 'desinfektioaine', 'dokumentaarinen taide', 'duaalikappale', 'edustajaeläke', 'ehoste', 'ehtolause', 'eine', 
                   'eksistentiaalilause', 'elake', 'ele', 'elintarvike', 'eliömaantiede', 'elje', 'elkeet', 'elokuvataide', 'elolähde', 'elorinne', 
                   'eläinlääke', 'eläke', 'energiajäte', 'enne', 'enne', 'ennuste', 'erhe', 'eriste', 'erite', 'erityisesine', 'esine', 'este', 
                   'ettone', 'etuliite', 'etäkoe', 'etäpesäke', 'euroalue', 'ferriyhdiste', 'ferroyhdiste', 'haaste', 'haave', 'haje', 'hakarinne', 
                   'hakasulje', 'hake', 'hallituspuolue', 'halme', 'halme', 'hame', 'hammaste', 'hanke', 'hapate', 'hapoke', 'harjanne', 'harjoite', 
                   'harmaahylje', 'harraste', 'harteet', 'hattupuolue', 'haude', 'hautajaiskulkue', 'hautajaispuhe', 'hautajaissaattue', 
                   'havumetsävyöhyke', 'havurinne', 'hebrealaiskirje', 'hede', 'heijaste', 'heikenne', 'heite', 'hele']

    
LIST_SUFFIX = ["nsa", "nsä", "lle", "mme", "nne", "tse", "sti", "lti"]

def warning_detect(transcript, prediction):
    # transcript was replace with PAD symbol, need to revert back
    transcript = transcript.replace('|', ' ')      

    listWarning = []
    
    #Detect transcript too short and contain ÄÄ
    # 
    if (
            len(transcript) <= 4 and (len(prediction) == 0 or "arois" in prediction or "arvo" in prediction) 
        or( len(transcript) <= 3 and re.search(RE_DOUBLE_VOWELS, transcript))
        or( re.search(RE_QUAD_VOWELS, transcript))
       ):
        listWarning.append(WARNING_CODE["AAA"])
    
    #Detect mp
    if ("np" in transcript): 
        listWarning.append(WARNING_CODE["NP"])
        
    #Detect ng/nk
    if ("ng" in transcript or "nk" in transcript): 
        listWarning.append(WARNING_CODE["NGK"])
    
    # Detect MENEP
    # Boundary gemination or Loppukahdennus
    #https://uusikielemme.fi/finnish-grammar/phonology/boundary-gemination-loppukahdennus-advanced-finnish
    #https://jkorpela.fi/suomi/cab.html
    
    # Manual work since libvoikko is outdate and can't be installed in Red Hat Server
    for word in LIST_IMPERATIVE:
        if (re.search(word + " " + RE_CONSONANT, transcript)):
            listWarning.append(WARNING_CODE["MENEP"])
            break
    # This is for suffix cases
    if (WARNING_CODE["MENEP"] not in listWarning):
        for suffix in LIST_SUFFIX:
            if (re.search(suffix + " " + RE_CONSONANT, transcript)):
                listWarning.append(WARNING_CODE["MENEP"])
                break
    
    return listWarning

def textSanitize(transcript, addPad = True):
    # This is done in Unity instead
    
    #text = text.upper()
    #transcript = transcript.replace('\n', ' ')        
    #transcript = transcript.replace('-', ' ')
    #transcript = transcript.replace('z', 'ts')     
    #transcript = " ".join(transcript.split())
    if addPad:
        transcript = transcript.replace(' ', '|')        

    return transcript

def get_trellis(emission, tokens, blank_id=0):
    """
    Adding 'audio' in form of float numpy array to batch dataset. 
    Used together with map_to_w2v2_prediction for batch train/evaluation
    
    Args:
        batch:
            batch file containing at least 'path' which store the dicrectory of audio file
        sampling_rate:
            Target sampling rate for librosa to resampling
        path_column:
            Store the dicrectory of audio file
    """
    
    num_frame = emission.size(0)
    num_tokens = len(tokens)   

    # Trellis has extra dimensions for both time axis and tokens.
    # The extra dim for tokens represents <SoS> (start-of-sentence)
    # The extra dim for time axis is for simplification of the code.
    trellis = torch.full((num_frame + 1, num_tokens + 1), -float("inf"))
    trellis[:, 0] = 0
    
    for t in range(num_frame):
        trellis[t + 1, 1:] = torch.maximum(
            # Score for staying at the same token
            # Score = P(timeframe T) * P(timeframe T | PADDING)
            trellis[t, 1:] + emission[t, blank_id],
            # Score for changing to the next token
            # Score = P(timeframe T) * P(timeframe T | TOKENS)
            # We do not include the last token since it is the result of changing the token
            # the last token P is equal to P(token-1)*P(change token)
            trellis[t, :-1] + emission[t, tokens],
        )
    return trellis 

@dataclass
class Point:
    token_index: int
    time_index: int
    all_score: float
    token_score: float

def backtrack(trellis, emission, tokens, extra_frame=6, blank_id=0, space_id=0):
    # Note:
    # j and t are indices for trellis, which has extra dimensions
    # for time and tokens at the beginning.
    # When referring to time frame index `T` in trellis,
    # the corresponding index in emission is `T-1`.
    # Similarly, when referring to token index `J` in trellis,
    # the corresponding index in transcript is `J-1`.
    j = trellis.size(1) - 1
    t_start = torch.argmax(trellis[:, j]).item()
    
    # Extend the path to 2 more extra_frame at the end
    t_start += extra_frame
    
    # If the last phone is also at the end of the sound frame, there's no extra padding at the end
    #to avoid index out of bounds
    if t_start>=trellis.size(0):
        t_start = trellis.size(0) - 1
    
    count_down = extra_frame
    start_cd = False

    path = []
    for t in range(t_start, 0, -1):
        # 1. Figure out if the current position was stay or change
        # Note (again):
        # `emission[J-1]` is the emission at time frame `J` of trellis dimension.
        # Score for token staying the same from time frame J-1 to T (which mean they are padding).
        padding_score = trellis[t - 1, j] + emission[t - 1, blank_id]        
        # Score for token changing from C-1 at T-1 to J at T.
        # If model failed to put token into 1 framelength, then token_score could 
        # have several frame length
        token_score = trellis[t - 1, j - 1] + emission[t - 1, tokens[j - 1]]
        #print(f'index {t-1}, token {j} padding_score {padding_score}, token_score {token_score}, prob {emission[t - 1, tokens[j - 1]].exp()}')
        
        # 2. Store the path with frame-wise probability.
        all_prob = (emission[t - 1, tokens[j - 1] if token_score > padding_score else blank_id]).exp().item()
        token_prob = emission[t - 1, tokens[j - 1]].exp().item()
        
        # Return token index and time index in non-trellis coordinate.
        path.append(Point(j - 1, t - 1, all_prob, token_prob))

        # 3. Update the token
        if token_score > padding_score:
            
            # Extend the path to include two more frames at the beginning
            if (~start_cd) and (j == 1): 
                start_cd = True
                
            else:
                j -= 1
                
        # No longer meed this check since when j==1 and j-=1 mean it will break as soon as it reach j = 0
        # And since we extend two more frames --> it will break when cd end at 0
        #    if j == 0:
        #        break
        
        # This is to make sure j reach 0 
        # Because we insert count down
        # With this, we no longer need j == 0 check (since start cd --> j will reach 0 in 2 additional steps)
        if start_cd:
            count_down -=1
        if count_down == 0:
            break
    else:
        print("Failed to align")
        return path[::-1]
        #raise ValueError("Failed to align")
    return path[::-1]

@dataclass
class Segment:
    label: str
    start: int
    end: int
    score: float

    def __repr__(self):
        return f"{self.label}\t({self.score:4.2f}): [{self.start:5d}, {self.end:5d})"

    @property
    def length(self):
        return self.end - self.start

def merge_repeats(transcript, path, ignore_pad=True):
    # Merge the score, calculate scoring based on path    
    i1, i2 = 0, 0
    segments = []
    while i1 < len(path):
        while i2 < len(path) and path[i1].token_index == path[i2].token_index:
            i2 += 1
        
        if ignore_pad:
            # Since padding score is ignore, token score is calculated as maximum value of token:            
            score = max(path[k].token_score for k in range(i1, i2)) 
        else:            
            # If padding is included, score is calculated as average highest score 
            # of both padding and token accross the path
            score = sum(path[k].all_score for k in range(i1, i2)) / (i2 - i1)
           
        segments.append(Segment(transcript[path[i1].token_index],
                                path[i1].time_index, 
                                path[i2-1].time_index + 1, 
                                score))
        i1 = i2
    return segments          