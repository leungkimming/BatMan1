# Introduction 
TODO: Give a short introduction of your project. Let this section explain the objectives or the motivation behind this project. 

# Getting Started
Useful GIT operations
revert git to a particular commit
git merge --abort < needed if git pull origin master failed with incomplete merge >
git pull origin master
git reset --hard bb76992583a59722d3b9a3690a750ed9d07a4143
git push -f origin master
git branch --list //list local branches you have checkout
git checkout -b Dev //switch to a branch
git pull origin Dev //get source from the new branch
remove all histories
md grid-mr-cont
cd grid*
git init
git remote add -m master origin https://GRID-MR@dev.azure.com/GRID-MR/GRID-MR-CONT/_git/GRID-MR-CONT
git pull origin master
<login>
git checkout --orphan temp 9ac0c4e4c121aabef2b7f3202fac9a4af5bda4c8
git commit -m "remove history"
git rebase --onto temp 9ac0c4e4c121aabef2b7f3202fac9a4af5bda4c8 master
git branch -D temp
git gc --prune=all
git repack -a -f -F -d
git push origin master -f
git init
git remote add origin <url to the new repo> e.g. https://AMIPoC@dev.azure.com/AMIPoC/NEWProject/_git/NEWProject 
git pull origin master
git push origin master