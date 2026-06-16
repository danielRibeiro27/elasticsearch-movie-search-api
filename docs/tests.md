# Tests That Tells When its Done

## Unit Tests

### CSV Parser
Given valid csv
Should parse csv correctly

Given invalid csv
Should handle error

Given missing title in row
Should skip it and move to the next

## Integration Tests

~~None~~

## Functional E2E Tests

**POST /seed**
Should return 200 ok

**GET  /search?q=matrix&year=1999&language=en&minRating=8**
Should return 200 ok
Should be completed in under 5ms

**GET  /autocomplete?q=matrx**
Should return 200 ok
Should return data with string "matrix"

**POST /reindex**
Should return 200 ok

**GET  /movie/{id}**
Should return 200 ok
Should return data with same id as requested

## Non-Functional Requirements

### Search Performance

**GET /search?q=matrix**

Should complete in under 50ms for a catalog containing at least 100,000 movies on local development hardware.
The Elasticsearch search endpoint should execute
significantly faster than an equivalent SQL LIKE query.

Record:
- SQL execution time
- Elasticsearch execution time

### Autocomplete Performance

**GET /autocomplete?q=mat**

Should complete in under 20ms for a catalog containing at least 100,000 movies.

