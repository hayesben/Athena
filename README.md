# Athena

Athena is a word embedding program based on the original paper *Efficient Estimation of Word Representations in Vector Space* published by Tomas Mikolov in January 2013.

This is a GPU accelerated C# implementation, which provides a full environment to manage a large text corpus and subsequently learn and query word embeddings. To the best of my knowledge, this is the only implementation of word2vec on a GPU using C#.

Athena works with either CUDA or OpenCL devices and will auto-select depending on your hardware. A reference to Cudafy.NET is required, which can be downloaded from [GitHub](https://github.com/svn2github/cudafy).  

To get started, select the ***Load [L]*** option which will load your corpus file located in sub folder named ***Data*** under compiled application folder – I use a 6GB full text dump of Wikipedia - this file must be called ***corpus.txt***.

Athena then cleans up the corpus file, transforming text to lower case, standardising diacritics and converting numerics to tokens - the resulting text will be saved as ***corpus_0.txt***.

Next, Athena will identify common recurring bigrams and concatenate these into phrases – the result will be saved as ***corpus_1.txt*** - this process is heuristic and may therefore omit some bigrams. To guarantee the occurrence of particular bigrams in your corpus it is possible to force addition - create a ***bigrams.txt*** file and populate each row with a required bigram in the form of *hillary_clinton*, *donald_trump*, etc.

The proceeding steps, which will take several hours to run, need only be executed once – now the training can begin.

By selecting the ***Train [T]*** option, Athena will use the cleaned corpus file to generate a word embedding model on the GPU – this will be stored in ***model.bin***. If training on this corpus has taken place before, Athena will load the existing vocabulary.  If not, Athena will first learn the vocabulary and start training from here.

To query the model, simply select the ***Query [Q]*** option and type in a word or phrase.

For example, typing in the word *paris* will return cities similar to Paris:

```
? paris

Nearest
-------------------
1.00  paris
0.81  brussels
0.73  vienna
0.71  munich
0.71  marseilles
0.70  turin
0.69  strasbourg
0.69  madrid
0.69  geneva
0.68  london
```

It is also perform vector subtraction by appending a colon to the end of the word to negate. For example *paris: france oslo* is the equivalent of asking Athena *Paris is to France as Oslo is to...?* – this should return *Norway* as the top answer:

```
? paris: france oslo

Nearest
-------------------
0.87  norway
0.78  finland
0.75  denmark
0.75  iceland
0.74  sweden
0.73  finnmark
0.72  trondheim
0.71  tromso
0.71  estonia
0.71  oslo
```

To carry out an exhaustive test of the model, select the ***Test [E]*** option. This will load comma seperated analogies from the ***test.csv*** file. These should be in the form of *athens,greece,baghdad,iraq*.

Let me know how you get on...

@robosoup
www.robosoup.com
