import pandas as pd

full_df = pd.read_csv("gladsheim_dataset.csv", sep="\t")

full_df["isBot"] = full_df["isBot"].apply(lambda x: x == "true")
full_df["timestamp"] = full_df["timestamp"].apply(pd.to_datetime)

df = full_df[(~full_df["isBot"]) & (~full_df["messageText"].isna())]
