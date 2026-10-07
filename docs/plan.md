## Baby-step plan

This spec should be built in the smallest possible sequence of working slices.

## Phase 1 — Skeleton

1. Create the .NET 9 Web API project.
2. Add the folders:

   * `Controllers`
   * `Services`
   * `Models`
   * `data`
   * `docs`
3. Create the `Movie` model with only the fields needed for search:

   * `Id` — required
   * `Title` — required
   * `Overview`
   * `Year`
   * `Language`
   * `Rating`
4. Add Elasticsearch connection settings in `appsettings.json`.

**Done when:** the API runs and the project structure exists.

## Phase 2 — Hardcode one movie and one index

5. Implement `ElasticSearchService`.
6. Add the code to:

   * connect to Elasticsearch
   * create the movie index
   * insert one movie document
   * fetch one movie by `id`
7. Expose a temporary test endpoint or use `/seed` to insert a single record.

**Done when:** you can create and retrieve one document from Elasticsearch.

## Phase 3 — Seed from CSV

8. Put `movies.csv` in `local-data`.
9. Parse the CSV.
10. Map CSV rows into `Movie` objects.
11. Bulk insert the records into Elasticsearch through `/seed`.

**Done when:** the index contains real movie data from the CSV.

## Phase 4 — Basic search

12. Implement `GET /search?q=`.
13. Make it search by:

* title
* overview

14. Return a simple list of movies.

**Done when:** searching `"matrix"` returns relevant results.

## Phase 5 — Performance Test (main purpose)

15. Create a postman request and check if the latency is satisfable.

## Phase 6 — Search filters

15. Add optional query filters:

* `year`
* `language`
* `minRating`

16. Combine text search + filters in the same Elasticsearch query.

**Done when:** `/search?q=matrix&year=1999&language=en&minRating=8` works.

## Phase 7 — Autocomplete

17. Add autocomplete support in the index mapping.
18. Implement `GET /autocomplete?q=`.
19. Return only the smallest useful fields:

* `id`
* `title`

**Done when:** typing partial titles gives suggestions fast.

## Phase 8 — Reindex flow

20. Implement `POST /reindex`.
21. Make it:

* delete the current index
* recreate the index
* reinsert all CSV data

**Done when:** you can rebuild the entire search index from scratch.

## Phase 9 — Movie by ID

22. Implement `GET /movie/{id}`.
23. Fetch the movie directly from Elasticsearch by document id.

**Done when:** a single movie page/detail lookup works.

## Phase 10 — Minimal polish

24. Add basic error handling.
25. Add empty-state responses:

* no results
* invalid id
* seed/reindex failure

26. Add a short `docs/README.md` explaining:

* what Elasticsearch solves here
* how to run the project
* how to seed and search

**Done when:** another person can run the project without guessing.

---

## Summary

1. Project skeleton
2. Elasticsearch connection
3. One hardcoded insert/retrieve
4. CSV seed
5. Performance Test
6. Search endpoint
7. Filters
8. Autocomplete
0. Reindex
10. Movie by ID
11. README

---

## Definition of a good MVP

The project is good enough when all of this is true:

- [ ] You can seed the index from CSV.
- [ ] You can search by title/overview.
- [ ] You can filter by year/language/rating.
- [ ] You can autocomplete partial titles.
- [ ] You can reindex the catalog.
- [ ] You can fetch one movie by ID.
