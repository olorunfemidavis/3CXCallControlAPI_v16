using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("create_prompt_set")]
    [SampleDescription("Synthetic sample. It shows how to configure PromptSet object.")]
    class CreatePromptSetSample : ISample
    {
        public void Run(params string[] args)
        {
            using (var myPromptSet = PhoneSystem.Root.CreatePromptSet("My Custom PromptSet", "folder in IVR prompts", "dn", "dn"))
            {

                myPromptSet.UseAlternateNumberPronunciation = true;
                myPromptSet.Version = "0.0.1";
                myPromptSet.Description = "Synthetic PromptSet";
                myPromptSet.PromptSetType = PromptSetType.Custom;
                System.Collections.Generic.List<Prompt> myListOfPrompts = new System.Collections.Generic.List<Prompt>();
                for (int i = 0; i < 100; i++)
                {
                    Prompt newPrompt = myPromptSet.CreatePrompt();
                    newPrompt.ID = "PRMPT" + i.ToString();//set string ID of prompt
                    newPrompt.Filename = "nameoffileinpromptsfolder" + i.ToString() + ".wav";//the name of file in prompts folder
                    newPrompt.Transcription = "text of prompt " + i.ToString();//description of prompt you can copy it from 
                    myListOfPrompts.Add(newPrompt);
                }
                myPromptSet.Prompts = myListOfPrompts.ToArray();
                myPromptSet.Save();
                System.Console.WriteLine("Prompt is Saved Successfully");
                myPromptSet.Delete();
                //Following lines of code sets this promtset as default in phone system:
                //Parameter p = PhoneSystem.Root.GetParameterByName("ACPRMSET");
                //p.Value = myPromptSet.Folder;
                //p.Save();
            }
        }
    }
}
