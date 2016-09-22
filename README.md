# Athena

Athena is a word embedding program based on the original paper *Efficient Estimation of Word Representations in Vector Space* published by Tomas Mikolov in January 2013.

This is a **C#** implementation, which provides a full environment to manage a large text corpus and subsequently learn and query word embeddings.

To get started, select the ***Load [L]*** option which will load your corpus file located in the same directory as the compiled application – I use a 6GB full text dump of Wikipedia - this file must be called ***corpus.txt***.

Athena then converts the corpus file to lower case, standardises diacritics and converts numerics to standard tokens - the resulting text will be saved as ***corpus_0.txt***.

Next, Athena will identify recurring terms and concatenate these into phrases – the result will be saved as ***corpus_1.txt***. This process is heuristic and thus may omit some bigrams. To guarantee the occurrence of particular bigrams in your corpus it is possible to force addition - create a ***bigrams.txt*** file and populate each row with a required bigram in the form of *hillary_clinton*, *donald_trump*, etc.

The proceeding steps, which will take several hours to run, need only be executed once – now the training can begin.

By selecting the ***Train [T]*** option, Athena will use the cleaned corpus file to generate a word embedding model – this will be stored in ***model.bin***. If training has taken place before, Athena will attempt to load the existing model and continue training from the current state.  If not, Athena will learn the vocabulary and build a seeded, but untrained, model file and start training from here.

To query the model, simply select the ***Query [Q]*** option and type in a word or phrase.

For example, typing in the word *write* will return related words and also its context.

```
? write

Neighbours                              Context
-------------------                     -------------------
1.00  write                             0.51  to
0.80  compose                           0.40  would
0.78  memorize                          0.39  could
0.76  teach                             0.36  must
0.76  publish                           0.33  might
0.75  do_something                      0.29  does_not
0.75  deliver                           0.28  may
0.74  transcribe                        0.28  can
0.73  submit                            0.27  did_not
0.73  learn                             0.26  wouldnt
```

It is also perform vector subtraction by appending a colon to the end of the word you want to negate. For example *france: paris italy* is the equivalent of asking Athena *France is to Paris as Italy is to...?* – this should return Rome.

To carry out an exhaustive test of the model, select the ***Test [E]*** option. This will load comma seperated analogies from the ***model.csv*** file. These should be in the form of *athens,greece,baghdad,iraq*.

Let me know how you get on...

@robosoup
www.robosoup.com
