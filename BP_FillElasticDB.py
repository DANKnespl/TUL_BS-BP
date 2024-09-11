from elasticsearch import Elasticsearch
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

mapping = {
    "mappings": {
        "properties": {
            "DBCounter": {"type": "long"},
            "RecordCounter": {"type": "long"},
            "Text": {"type": "text", "fields": {"wildcard": {"type": "wildcard"}}}
        }
    }
}

def fillElastic(source,elasticDB,es):
    """
    Function to add new records to elasticsearch DB
        source - Line JSON file. Records NEED to be split by newlines.
        elasticDB - Index to which are the data added.
        es - Elasticsearch connection.
    """
    i = 0
    if not es.indices.exists(index=elasticDB):
        es.indices.create(index=elasticDB, body=mapping)

    with open(source,"r",encoding="utf-8") as raw_data:
        json_docs = raw_data.readlines()
        for json_doc in json_docs:
            i += 1
            es.index(index=elasticDB, id=i, document=json_doc)

if __name__=="__main__":
    es = Elasticsearch([{'host': 'localhost', 'port': 9200,'scheme':'https'}],basic_auth=('elastic','elastic'),ca_certs=False,verify_certs=False)
    fillElastic('D:/Elastic/NOTOG.json',"bp_data",es)