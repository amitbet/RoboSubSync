## Introduction   
Every once in a while I get an idea, which I just can't get out of my mind...

This program started with such an idea, and developed into a subtitle sync studio which doesn't involve any interaction with the actual movie at all. Sounds strange? read on.  

![application image](https://github.com/amitbet/robosubsync/raw/master/images/sub-studio.jpg "robo sub sync in action")


## Background  
The idea goes something like this: you have a movie and you have a subtitle in your language of choice, but you find out that it is not synchronized to the version of the movie you have downloaded, this may happen when the movie is old, or when it is too new, and sometimes even when there is a subtitle that should be synced to the same version but it actually isn't.

the is also usually an existing subtitle in English (or another language) which is in sync, translating it automatically will obviously be a bad idea, but one might think that if you have a synced sub and a language sub there will be some way to take the sync and transfer it, thus creating a good version of the subtitle - now, for my next trick, I'll do that, and you - grab some popcorn... 

Besides helping with bad sync this method can also help with synchronizing the subtitles to multiple video versions in the first place (given an english ver already exists), which is currently a rather tedious job. 




To create a comparison between the two subtitles, we need to get them into the same language first, Google translate was the first thing I thought of, so for my first version I just copied the translated subtitle in, and out it came in English, after that matching the lines was just writing code, my first results were very encouraging, and I went on to implementing several algorithms and methods that improve the results and give you better control over the end result.   

I later messed around with microsoft's translation and got it to be automatic, if you want to use it read the start of the linked article and register for Microsoft translation.  

## The User's guide  
To automatically sync a subtitle (the short version):

1. Insert the English .srt path in the first textbox  
1. Insert the language .srt in the second textbox 
1. Choose the correct encoding for your language srt file 
1. Press add translation (if you have registered to Microsoft translation put the passwords in the MS translation config expander and skip to step 7) 
1. Open str file, copy content into Google translate (check translation was done until the end of the text)  
1. Insert the Google translate English translation of your srt content into the open window 
1. Close the translation window 
1. Click AutoSync  
1. Click Save Srt File (on the bottom right corner)
1. Enjoy! 
Now that we have satisfied our results craving, we can make some room for curiosity... 
There are many options and playing with it can get you even better results than just letting the program have its way with things.  

What actually happens here is: 

1. The translated lines are fuzzy-matched to the English (synced) subtitle 
1. The auto sync button tries several values, and uses the values which produce the most line matches. 
1. A baseline is created (the blue line) 
1. Any abnormal matches (too far from the baseline), are discarded.
1. Line and baseline are then drawn 
1. The save button is pressed  
1. The language subtitle lines are corrected according to the line match line 
1. The fixed srt file is saved to disk called fixed_<TheOriginalName>.srt  

This will usually give you a good result but if you want to make it better, there are several levers, nobs and wiggles you can play with: 
### The lines in the graph: 
* **Sync Difference (purple)** - each dot in this graph represents one match, the value is the substraction (SyncedSubTime - LanguageSubTime), which when added to the language subtitle will correct it to be in sync with the video. 
* **Baseline (blue)** - the moving baseline created by the baseline algorithem is similar to a moving  average line, and is used to weed out outliers, removing most incorrect matches. 
* **Step line (orange)** - this algorithm goes by the assumption that there were different commercial breaks in the movie versions, or that one of them was otherwise composed from several parts, it tries to find steps where sync diff is constant and then jumps to another constant level. 
* **Linear regression line (green)** - the straight line that has the least distance from all the points, this line will be close to the purple match line, if the sync diff is growing (+/-) in a constant speed. (like when you have differences in frame rates)   
### The parameter sections:  
Most of the parameters in these sections require you to press "update graph" to apply changes, graphs will then be recalculated and redrawn. 

* **Line match parameters** - gives you control over the fuzzy matching algorithm, including similarity percentage required for match, minimal letters, and search ahead slack space. 
* **Baseline Parameters** - allows you to adjust the way the baseline is created, whether it is used to remove the abnormal points, and how far a point should be to be called abnormal (normal zone is measured in standard deviations away from the baseline). 
* **Microsoft Translation Config** - a place to put the MS registration if you get one (it's free) 
* **Step Line Parameters** - lets you fine tune the step line, including the number of steps to try and create, the number of points which seem to belong to the next step before stepping etc. 
* **Fix & save subtitle** - here you can choose which line will be used to fix the subtitle, including the original (sync diff), baseline, step, and regression lines. 
### Edit on the fly:   
Another option is to manually fix the sync diff line, according to your own approximation of the diff, in order to accommodate this usage, I added the ability to drag the sync diff line's vertices up and down. During the dragging, a tooltip will display the two lines (English and local language), so you can judge the fit.
If the lines look like a bad match, dragging the point back into the slope created by its neighbors will eliminate its effect on the sync. 

### Graph navigation  
The graph is based on the DynamicDataDisplay project, it can be paned with the mouse, zoomed with scroll wheel or +/- keys, and right clicking on it will show a helpful menu.  
If graph gets scaled out of shape, pressing "home" on your keyboard will get you back to the initial position. 
  
## The Code  
Some parts of the code were refactored repeatedly until i got a simple way of treating the line data, which eliminated worrying about lines, 

**SampleCollection** is a class that represents a sync correction line, it contains the match points and can apply algorithms to calculate new lines (returning a new SampleCollection instance), which lets you create calculation chains without worrying about repercussions. It can also transfer its data into a graph's DataSource for display purposes. 

**BingTranslator** - encapsulates the usage of Microsoft's translation services.

**MainVM** -  ViewModel for the main window, holds most of the logic, and a ton of observable properties... (a good starting point is SyncSubtitles) 

**LineInfo** - represents a line in the subtitle (some times 2 or three are contained in one TimeStamp) 

**TimeStamp** - holds a subtitle time along with a list of lines 

**SubtitleInfo** - represents a subtitle file with all it's content (times and lines), can also load and save srt. 

**Baseline** - a class for the baseline algorithm. (used by sampleCollection) 

## Algorithms  
There are several algorithms in use here:   
**Fuzzy Match**: this is just something i cooked up to do the work:  

Try to match a line to it's counterpart:
* for each word from the translated sub line
    * If contained in the line synched line
        * count += word letters. 
* Calculate letter hit percentage etc.  
* Check the match against thresholds, to see if its a good one: 
    * If match, advance to the next line in both subs 
    * If not, advance one line in the translated sub, and retry (creating a shift) 
* Switch subtitles try it with the translated as the fixed sub.
* Take the match with the most lines.  

**Baseline algorithm**: done by an adaptive average alg, which takes the average previously achieved as a part of the new average and the rest as the remaining part, nextAvg = [alpha*avg + (1-alpha)*newPoint] so alpha decides the closeness in which the algorithm follows the diff line. 


**Step Detection Algorithm**: clustering (k-means) is used to find groups of points with a similar y axis:

* Divide the space between the highest Y value and the lowest into K+1 (configurable) equal sections 
* Initialize the means vector with these K lines 
* Until [no change in means vector]:  
    * for each point
        * Find nearest mean 
        * Add to nearest mean accumulator 
        * Increment nearest mean's count 
    * For each mean: (update means) 
        * mean = meanAccumulator / meanCount  

Now each mean (= horizontal line) should have aligned it's self with the nearest step. 

To create the step line we now need to decide when to move from one step to the next, this is done by going over the points, and deciding to step when we see X points which belong to the next cluster. (here we can demand points to be consecutive, thus using a more strict approach when the data is messy) 

**Linear regression** is done by using a simple Linear least squares algorithm.  

## Future Work 


There are several improvements and stuff on my todo list for this program:  

English sub may be downloaded automatically from open subs etc.  
Auto encoding detection can be used (there's a good article here)  
.sub file support (the easiest way is writing a convert sub to srt routine, that receives the frame rate)  
This program only works with srt file for now, other popular formats can be converted to srt.  
supporting a non-English language as common language (maybe translating both subtitles to English, or translating language sub into a different common language)  
Adding a commandLine interface or batch support for auto synchronization of multiple subtitles.  
Encapsulating the editable graph component  

## Conclusions 


I like a good challange once in a while, and this one was fun to tackle, I think that this can greatly benefit the people who create subtitles, as well as people like me, that just got stuck with an out-of-sync subtitle once in a while.  

Tell me what you think, and what you would like to see in the next version, in the comments below. 

If you like this program, and want to add to it, you can contribute changes to the RoboSubSync codeplex repo.  