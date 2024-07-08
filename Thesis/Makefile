all: thesis abstract-cs.pdf abstract-en.pdf

thesis:
	pdflatex .\thesis.tex -shell-escape -halt-on-error && biber thesis && pdflatex .\thesis.tex -shell-escape -halt-on-error && pdflatex .\thesis.tex -shell-escape -halt-on-error

%.pdf: force
	latexmk $*.tex

clean:
	rm -rf aux
	rm -f thesis.pdf abstract.pdf abstract-cs.pdf abstract-en.pdf

.PHONY: force
