"""
Script to generate JSON with data from given dir
"""
import json
from pathlib import Path
#import numpy as np
import os


def getJson(srcPath,destPath,ES_index):
    """
    Function to generate JSON file with data from given directory
        srcPath - path to folder with image and text files with same names.
        destPath - path to file to write JSON data.
        ES_index - index in which data should be in elasticsearch DB
    """
    folders = [f for f in os.listdir(srcPath)]
    DBListName="Main"
    DBList=[0 for i in range(len(folders))]
    DB="Year"
    record="Page"
    j=0
    for fold in folders:
        DB_Name=fold[1:]
        inh=""
        txts = [f for f in os.listdir(srcPath+"/"+fold) if f.endswith(".txt")]
        arr=[0 for i in range(len(txts))]
        for i in range(len(txts)):
            txts[i] = srcPath+"/"+fold+"/"+txts[i]
            if txts[i].find("inhalt")!=-1:
                inh=txts[i]
            if txts[i].find("MISS")==-1:
                arr[i]=[txts[i],txts[i].replace(".txt",".jpg"),"0","0"]
            else:
                arr[i]=[txts[i],"","0","0"]
        output={
            "DB_Name": DB_Name,
            "DB_Type": DB,
            "Record_Type": record,
            "Lookup_File": inh,
            "Records": arr
        }
        DBList[j]=output
        j+=1

    ou={
        "DB_List_Name":DBListName,
        "ES_index":ES_index,
        "DB_List":DBList
    }
    json_object = json.dumps(ou, indent=4,ensure_ascii=False)
    try:
        with open(destPath, "w",encoding='utf8') as outfile:
            outfile.write(json_object)
    except:
        print("fold")

if __name__== "__main__":
    getJson("D:/Rocenky_JJHS","D:/BP_DBFile/BP_Knespl.json","bp_data")
