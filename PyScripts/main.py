import pandas as pd
import unidecode
import re
import json
import emoji


def extract_emojis(s):
    return ''.join(c for c in s if c in emoji.UNICODE_EMOJI)


# df_all = pd.read_csv("gladsheim_dataset.csv",sep="\t")
# df = df_all[(~df_all["messageText"].isna()) & (~df_all["quotes"].isna())][["quotes", "messageText"]].rename(columns={"quotes":"message", "messageText": "reply"})
#
# # TODO: remove "(edited)"
# # TODO: remove urls
#
# df["message"] = df["message"].apply(lambda x: re.sub(r'https?:\/\/\S*', '', x)).apply(lambda x: re.sub(r'\(edited\)', '', x)).apply(lambda x: unidecode.unidecode(x)).apply(lambda x: x.strip())
# df["reply"] = df["reply"].apply(lambda x: re.sub(r'https?:\/\/\S*', '', x)).apply(lambda x: re.sub(r'\(edited\)', '', x)).apply(lambda x: unidecode.unidecode(x)).apply(lambda x: x.strip())
#
# df = df[((~df["message"].isna()) & (df["message"] != "")) & ((~df["reply"].isna()) & (df["reply"] != ""))]
#
# # df.to_csv("tmp.csv", header=False, index=False)
# # with open("tmp.csv", "r") as file:
# #     data = file.read().replace('\n', '')
#
# with open("gladsheim_chat.txt", 'w') as out_file:
#     for idx, row in df.iterrows():
#         out_file.write(row["message"]+"\n")
#         out_file.write(row["reply"]+"\n")

def extractMessage(message_json):
    return {
        "id": message_json["id"],
        "timestamp": message_json["timestamp"],
        "content": message_json["content"],
        "author_id": message_json["author"]["id"],
        "author_isBot": message_json["author"]["isBot"],
        "author_name": message_json["author"]["name"]
    }


with open("C:/Users/matej/Desktop/DiscordChatExporter/exported/4sgard - Text - gladsheim [138031010951593984].json",
          'r') as chat_file:
    chat_json = json.load(chat_file)

messages = map(extractMessage, chat_json["messages"])
df_all = pd.DataFrame(messages)
df_all["timestamp"] = df_all["timestamp"].apply(pd.Timestamp)

first_quote_timestamp = df_all[df_all["content"].str.startswith("> ")].sort_values("timestamp").iloc[0][
    "timestamp"]  # +- cas pridania quotovania
# first_quote_timestamp = 2017-01-03T20:09:28.194000000

# Phase 1: completely ignore quotes
df = df_all[df_all[
                "timestamp"] >= first_quote_timestamp]  # remove old posts (old being before "adding of the quoting feature" was chosen arbitrarily)
df = df[~df["content"].str.startswith("> ")]  # remove quotes
df = df[~df["content"].str.startswith(".")]  # remove bot commands #1
df = df[~df["content"].str.startswith("$")]  # remove bot commands #2
df = df[~df["author_isBot"]]  # remove bots

df["content_og"] = df["content"]
df["content"] = df["content"].apply(lambda x: re.sub(r'https?:\/\/\S*', '', x))  # remove urls

# remove tags
tags = list(df_all["author_name"].unique()) + ["@Film??ci", "@Bush did 911", "@Mod ???????", "@Vrcholn?? Legendy",
                                               "@Terraria Borci??", "@Dj", "@Onlajnovi Borci??", "@??ulc je p????a"] + [
           "@everyone", "@here"]

for tag in tags:
    df["content"] = df["content"].apply(lambda x: x.replace(tag, ""))

# Tokenize gladsheim emojis
gladsheim_emojis = {
    ":wrong:": " ??wrong?? ",
    ":whodid911:": " ??whodid911?? ",
    ":ts:": " ??ts?? ",
    ":triggered:": " ??triggered?? ",
    ":tinkerer:": " ??tinkerer?? ",
    ":tinfoil:": " ??tinfoil?? ",
    ":sulcW:": " ??sulcW?? ",
    ":sramekW:": " ??sramekW?? ",
    ":spedajsi:": " ??spedajsi?? ",
    ":skriW:": " ??skriW?? ",
    ":sinking:": " ??sinking?? ",
    ":seen:": " ??seen?? ",
    ":scorpW:": " ??scorpW?? ",
    ":respects:": " ??respects?? ",
    ":pica:": " ??pica?? ",
    ":penak:": " ??penak?? ",
    ":onlyfans:": " ??onlyfans?? ",
    ":OMEGALUL:": " ??OMEGALUL?? ",
    ":monke:": " ??monke?? ",
    ":MEGALUL:": " ??MEGALUL?? ",
    ":matkoW:": " ??matkoW?? ",
    ":matkoTip:": " ??matkoTip?? ",
    ":markoW:": " ??markoW?? ",
    ":lochW:": " ??lochW?? ",
    ":kys:": " ??kys?? ",
    ":kucaW:": " ??kucaW?? ",
    ":kara:": " ??kara?? ",
    ":invsinking:": " ??invsinking?? ",
    ":invcrucifix:": " ??invcrucifix?? ",
    ":invbackstab:": " ??invbackstab?? ",
    ":invalk:": " ??invalk?? ",
    ":HYPERLUL:": " ??HYPERLUL?? ",
    ":highnoon:": " ??highnoon?? ",
    ":gold:": " ??gold?? ",
    ":floralcoomer:": " ??floralcoomer?? ",
    ":fedorov:": " ??fedorov?? ",
    ":doubt:": " ??doubt?? ",
    ":domW:": " ??domW?? ",
    ":dolfW:": " ??dolfW?? ",
    ":cyklista:": " ??cyklista?? ",
    ":crucifix:": " ??crucifix?? ",
    ":CoolGuy:": " ??CoolGuy?? ",
    ":china:": " ??china?? ",
    ":breaded:": " ??breaded?? ",
    ":bombulus:": " ??bombulus?? ",
    ":bepis:": " ??bepis?? ",
    ":baconW:": " ??baconW?? ",
    ":backstab:": " ??backstab?? ",
    ":alk:": " ??alk?? ",
    ":4sg:": " ??4sg?? "
}

# emojis = df["content"].apply(extract_emojis)
# emojis_all = emojis.str.cat()
#
# emoji_freq_dict = {}
#
# for i in emojis_all:
#     if i in emoji_freq_dict:
#         emoji_freq_dict[i] += 1
#     else:
#         emoji_freq_dict[i] = 1
#
# popular_emojis = {key: value for (key, value) in emoji_freq_dict.items() if value > 150}  # 150 je z hlavy

# Mapping created manually from ^
# Tokenize discord emojis
discord_emojis = {
    '????': ' ??frowning?? ',
    '????': ' ??thinking?? ',
    '????': ' ??smile?? ',
    '????': ' ??ok_hand?? ',
    '????': ' ??joy?? ',
    '????': ' ??b?? ',
    '????': ' ??smirk?? ',
    '????': ' ??slight_smile?? ',
    '????': ' ??face_with_raised_eyebrow?? ',
    '????': ' ??person_shrugging?? ',
    '????': ' ??flushed?? '
}

#
# for ge_key, ge_value in gladsheim_emojis:
#     df["content"] = df["content"].apply(lambda x: x.replace(ge_key, ge_value))  # remove tags

# Tokenize punctuations
punctuations = {
    "!": " ! ",
    "?": " ? ",
    ".": " . ",
}

# Tokenize
for tokenizing_key, tokenizing_value in {**gladsheim_emojis, **discord_emojis, **punctuations}.items():
    df["content"] = df["content"].apply(lambda x: x.replace(tokenizing_key, tokenizing_value))

# Normalize accents
# Note: use this instead of unidecode.unidecode to keep my special tokenizing character '??'
accents = {
    "??": "a",
    "??": "e",
    "??": "i",
    "??": "o",
    "??": "u",
    "??": "y",
    "??": "c",
    "??": "d",
    "??": "e",
    "??": "n",
    "??": "r",
    "??": "s",
    "??": "t",
    "??": "z",
    "??": "u",
    "??": "a",
    "??": "l",
    "??": "l",
    "??": "o",
    "??": "r",
}

for normalizing_key, normalizing_value in accents.items():
    df["content"] = df["content"].apply(lambda x: x.replace(normalizing_key, normalizing_value))

whitelist = '0123456789abcdefghijklmnopqrstuvwxyz.!??? '  # space is included in whitelist


def filter_line(line):
    return ''.join([ch for ch in line if ch in whitelist])


df["content"] = df["content"].apply(lambda x: x.lower())
df["content"] = df["content"].apply(filter_line)
df["content"] = df["content"].apply(lambda x: x.strip())

df = df[df["content"] != ""]  # Remove messages, which are now empty
df = df.reset_index(drop=True)

thread_diff_threshold = pd.Timedelta(
    pd.offsets.Minute(30))  # vytvori novy thread, ak je rozdiel medzi spravami vacsi ako toto

df["started_new_thread"] = df["timestamp"] - df["timestamp"].shift() > thread_diff_threshold
df.loc[0, "started_new_thread"] = True
df["thread_id"] = df["started_new_thread"].cumsum()
df["message_group_id"] = (
        (df["thread_id"] != df["thread_id"].shift()) | (df["author_id"] != df["author_id"].shift())
).cumsum()

message_groups = []
for message_group_id, message_group in df.groupby(["message_group_id"]):
    message_group_started_thread = message_group.any()["started_new_thread"]
    message_group_content = message_group["content"].str.cat(sep=" ??tkn_newmsg?? ")
    message_group_replying_to = float('nan') if message_group_started_thread else message_group_id - 1
    message_groups.append({
        "group_id": message_group_id,
        "replying_to_group_id": message_group_replying_to,
        "content": message_group_content
    })
    # print(group)

df_grouped = pd.DataFrame(message_groups)
df_grouped["content"] = df["content"].apply(lambda x: x.replace("??", "|")) # replace with a safer character
df_q_a = pd.merge(left=df_grouped, right=df_grouped, left_on="replying_to_group_id", right_on="group_id",
                  suffixes=("_reply", ""))

with open("gladsheim_chat.txt", 'w') as out_file:
    for idx, row in df_q_a.iterrows():
        out_file.write(row["content"] + "\n")
        out_file.write(row["content_reply"] + "\n")
