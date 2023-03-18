## Recent
```dataviewjs
dv.table(["File", "Tags"], dv.pages('"Notes"').sort(p => -p.file.mtime).map(p => [p.file.link, p.file.tags.join(" ")]).slice(0,10))
```
## IDEA
```dataviewjs
let tag = "IDEA"
dv.table(["Name", "Tags"], dv.pages('"Notes" and #' + tag).sort(p => -p.file.mtime).sort(e => -p.priority).map(p => [p.file.link, p.file.tags.join(" ")]))
```

## TODO
```dataviewjs
let tag = "TODO"
dv.table(["Name", "Tags"], dv.pages('"Notes" and #' + tag).sort(p => -p.file.mtime).sort(e => -p.priority).map(p => [p.file.link, p.file.tags.join(" ")]))
```
## WIP
```dataviewjs
let tag = "WIP"
dv.table(["Name", "Tags"], dv.pages('"Notes" and #' + tag).sort(p => -p.file.mtime).sort(e => -p.priority).map(p => [p.file.link, p.file.tags.join(" ")]))
```
## MVP
```dataviewjs
let tag = "MVP"
dv.table(["Name", "Tags"], dv.pages('"Notes" and #' + tag).sort(p => -p.file.mtime).sort(e => -p.priority).map(p => [p.file.link, p.file.tags.join(" ")]))
```
## DONE
```dataviewjs
let tag = "DONE"
dv.table(["Name", "Tags"], dv.pages('"Notes" and #' + tag).sort(p => -p.file.mtime).sort(e => -p.priority).map(p => [p.file.link, p.file.tags.join(" ")]))
```
## CLOSED
```dataviewjs
let tag = "CLOSED"
dv.table(["Name", "Tags"], dv.pages('"Notes" and #' + tag).sort(p => -p.file.mtime).sort(e => -p.priority).map(p => [p.file.link, p.file.tags.join(" ")]))
```
## BUG
```dataviewjs
let tag = "BUG"
dv.table(["Name", "Tags"], dv.pages('"Notes" and #' + tag).sort(p => -p.file.mtime).sort(e => -p.priority).map(p => [p.file.link, p.file.tags.join(" ")]))
```
## FIXED
```dataviewjs
let tag = "FIXED"
dv.table(["Name", "Tags"], dv.pages('"Notes" and #' + tag).sort(p => -p.file.mtime).sort(e => -p.priority).map(p => [p.file.link, p.file.tags.join(" ")]))
```