"""
Script to generate JSON with data for elasticsearch DB
"""
import json
import os
import re

def createJson(srcPath,destPath):
    """
    Function to create JSON with data
        srcPath - path to folder with image and text files with same names.
        destPath - path to file to write JSON data.
    """
    with open(destPath, "w",encoding='utf8') as outfile:
        outfile.write("")
    folders = [f for f in os.listdir(srcPath)]
    j=0
    year=0
    for fold in folders:
        txts = [f for f in os.listdir(srcPath+"/"+fold) if f.endswith(".txt")]
        for i in range(len(txts)):
            text=""
            txts[i] = srcPath+"/"+fold+"/"+txts[i]
            try:
                with open(txts[i],"r", encoding="utf-8") as f:
                    text=f.read()
            except Exception:
                text=""
            output={
                "DBCounter": year,
                "RecordCounter":i,
                "Text":text
            }
            json_object = json.dumps(output, indent=4,ensure_ascii=False)
            try:
                with open(destPath, "a",encoding='utf8') as outfile:
                    outfile.write(json_object)
            except:
                print("fold")
            j+=1
        year+=1

def toLine(srcDest):
    """
    Function to make every JSON object in given file be on a single new line
        srcDest - path to JSON to be turned into line JSON
    """
    json_data=""
    with open(srcDest, "r",encoding="utf-8") as f:
        json_data=f.read()

    json_data=json_data.replace("\n","")
    json_data=json_data.replace("}{","},{")
    json_data=json_data.strip()
    json_data=re.sub(r"\s*\}\s*","}",json_data)
    json_data=re.sub(r"\s*\{\s*","{",json_data)
    json_data=re.sub(r"\s+"," ",json_data)
    json_data=re.sub(r"\}\s*,\s*\{","}\n{",json_data)
    open(srcDest, 'w').close()
    with open(srcDest, 'a',encoding="utf-8") as outfile:
        outfile.write(json_data)

def main(srcPath,destPath):
    """
    Function to create JSON file where every object is on new line, from directory
        srcPath - path to folder with image and text files with same names.
        destPath - path to file to write JSON data.
    """
    createJson(srcPath,destPath)
    toLine(destPath)

if __name__ == '__main__':
    main("D:/Rocenky_JJHS","D:/Elastic/BP_Knespl.json")

