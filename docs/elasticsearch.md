# Elasticsearch

## What is Elasticsearch?

* High data volume
* Full-text search
* Aggregations
* Geospatial search

## Why Use It?

* SQL text search is slow
* NoSQL databases are better suited for text search
* Built on top of Apache Lucene

## Characteristics

* Stores data as documents
* Schemaless
* Uses REST for its API
* Useful for ingesting production logs
* Can be considered a NoSQL database

## ELK Stack

* Elasticsearch
* Kibana
* Logstash

---

# Why Is It So Fast?

## Traditional Database Search

```sql
SELECT * FROM documents
WHERE body LIKE '%fox%'
```

* Database scans every row and character
* Latency: O(n)

## Apache Lucene Approach

* Spreads the data
* Organizes data around words instead of documents

### Data by Word

| Word  | Docs     |
| ----- | -------- |
| brown | 001, 002 |
| fox   | 001, 003 |
| quick | 001, 003 |

### Index Information

* Term frequency: how many times a word appears
* Word position within a document
* Rare words allow faster lookups

---

# Text Analysis Pipeline

## Analyzer

### Character Filter

* Removes special characters
* Removes control characters

### Tokenizer

* Splits text into tokens

### Token Filter

* Removes common words
* Handles plural forms and other token transformations

---

# Storage and Performance

## Immutable Segments

* Uses immutable segments
* Reads from memory instead of disk
* No locking required for reads

## Deletion Process

* Deletions are marked using tombstone files

## Background Merge

* Segments are merged in the background
* Deleted documents are permanently removed during merges

---

# Relevance Scoring

## BM25 Scoring Algorithm

* Term frequency (with saturation)
* Inverse document frequency (IDF)

---

# Scalability

## Sharding

* Data is distributed across multiple shards

---

# Search Execution

## Search Steps

1. Query phase
2. Fetch phase
