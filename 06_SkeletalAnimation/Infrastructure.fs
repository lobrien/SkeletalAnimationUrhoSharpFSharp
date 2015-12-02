namespace SkeletalAnimation

open System

module Assert = 

    let IsNotNull o = 
        if o = null then
            raise (new NullReferenceException()) 

    let IsTrue b = 
        if b = false then
            raise (new Exception("Assertion failed"))


