

import os
import yaml
import json

outfilename = 'dimensionsdata/dimensions_Impression.jsonl'
infilename = 'dimensionsdata/dimensions_Impression.yaml'

if os.path.exists(outfilename):
    os.remove(outfilename)

with open(infilename) as infile:
    data = yaml.safe_load(infile)

def makevaluedict(path, value):
    if value is None:
        return makevaluedict(path[:-1], path[-1])
    if len(path) == 1:
        return {path[0]: value}
    else:
        return {path[-1]: makevaluedict(path[:-1], value)}

def findmeaning(data):
    for value in data:
        if isinstance(value, dict):
            if "Meaning" in value:
                return value["Meaning"]
    return ""

def helper(f, data, path):
    if isinstance(data, dict):
        for key, value in data.items():
            helper(f, value, path + [key])
    elif isinstance(data, list):
        meaning = findmeaning(data)
        for value in data:
            if isinstance(value, str) or isinstance(value, int) or isinstance(value, float):
                valuedict = makevaluedict(path, str(value))
                f.write(json.dumps({"root": path[0], "meaning": meaning, "insteadof": [], "value": valuedict, "searchtext": str(value)}))
                f.write('\n')
            elif isinstance(value, dict):
                if "InsteadOf" in value:
                    insteadof = value["InsteadOf"][0]
                    f.write(json.dumps({"root": path[0], "meaning": meaning, "insteadof": insteadof, "value": makevaluedict(path, None), "searchtext": path[-1]}))
                    f.write('\n')
                else:
                    helper(f, value, path)

with open(outfilename, 'w') as outfile:
    # Parse the data string into yaml.
    helper(outfile, data, [])

