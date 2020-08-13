#pragma once
/// <summary>
/// The mid-layer for C++ backend and C# front-end 
/// </summary>
namespace Kernel
{
    /// <summary>
    /// exposed for the front-end
    /// </summary>
    public ref class Analyzer sealed
    {
    public:
		Analyzer();

		void InsertNonterminal(Platform::String^ _String);
		void InsertTerminal(Platform::String^ _String);
		void InsertProduction(Platform::String^ _String);
		void SetStartNonterminal(Platform::String^ _String);

		/// <summary>
		/// result described by string
		/// </summary>
		/// <returns></returns>
		Platform::String^ GetLLAnalysisSheet();
		Platform::String^ GetSLRDeductions();
		Platform::String^ GetLRDeductions();
		Platform::String^ GetLRAnalysisSheet();

		Platform::String^ LLAnalysis(Platform::String^ _String);
		Platform::String^ LRAnalysis(Platform::String^ _String);

	private:
		LRGrammar lrGrammar; // object for LR-grammar analysis
		LLGrammar llGrammar; // object for LL-grammar analysis
    };
}
