all: thesis abstract-cs.pdf abstract-en.pdf

thesis:
	pdflatex .\thesis.tex && biber thesis && pdflatex .\thesis.tex && pdflatex .\thesis.tex

%.pdf: force
	latexmk $*.tex

clean:
	rm -rf aux
	rm -f thesis.pdf abstract.pdf abstract-cs.pdf abstract-en.pdf

.PHONY: force
