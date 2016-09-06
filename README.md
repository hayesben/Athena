# Athena

Athena is a word embedding program based on the original paper "Efficient Estimation of Word Representations in Vector Space" published by Tomas Mikolov in January 2013.

This is a C# implementation, which provides a full environment to manage a large text corpus and subsequently learn and query word embeddings.

To get started, load a large text corpus into the same directory as the compiled application – I use a 6GB full text dump of Wikipedia - this file must be called "corpus.txt".

Athena then converts the corpus file to lower case, standardises diacritics and converts numerics to standard tokens - the resulting text will be saved as "corpus_0.txt".

Next, Athena will identify recurring terms and concatenate these into phrases – this file will be saved as "corpus_1.txt".

The proceeding steps, which will take several hours to run, need only be executed once – now the training can begin.

By selecting the training option, Athena will use the clean corpus file to create a word embedding model – this will be stored in "model.bin". If training has taken place before, Athena will attempt to load the existing model and continue training from the current state.  If not, Athena will learn the vocabulary and build a seeded, but untrained, model file and start training from here.

To query the model, simply select the load option and type in a word or phrase.

For example, typing in “london” will return cities similar to London.

You can also perform vector subtraction be appending a colon ":" to the word you want to negate. For example "france: paris italy" is the equivalent of saying "France is to Paris as Italy is to..." – this should return Rome.

Let me know how you get on...
