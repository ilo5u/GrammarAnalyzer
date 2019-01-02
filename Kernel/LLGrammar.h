#pragma once

typedef std::string String, Statement;
typedef std::vector<Statement> Statements;

/// <summary>
/// 文法类
/// </summary>
class LLGrammar
{
public:
	/// <summary>
	/// 记号
	/// </summary>
	struct Token
	{
		/* 类型 */
		enum class Type
		{
			TERMINAL,    // 终结符
			NONTERMINAL, // 非终结符
			INVALID
		};
		Type type;
		std::string attribute; // 表达式

		Token() :
			type(Type::TERMINAL), attribute({ '@' })
		{
			// 默认构造为记号Epsilon
		}

		Token(Type type) :
			type(type), attribute()
		{
		}

		Token(Type type, char ch) :
			type(type), attribute({ ch })
		{
		}

		Token(const Token& _Right) :
			type(_Right.type), attribute(_Right.attribute)
		{
		}

		Token(Token&& _Right) :
			type(_Right.type), attribute(_Right.attribute)
		{
		}

		Token& operator=(const Token& _Right)
		{
			type = _Right.type;
			attribute = _Right.attribute;
			return *this;
		}

		Token& operator=(Token&& _Right)
		{
			type = _Right.type;
			attribute = _Right.attribute;
			return *this;
		}

		/* 以下关系运算用于STL SET */
		bool operator==(const Token& _Right) const
		{
			return attribute.compare(_Right.attribute) == 0;
		}

		bool operator!=(const Token& _Right) const
		{
			return !(*this == _Right);
		}

		bool operator<(const Token& _Right) const
		{
			return attribute.compare(_Right.attribute) < 0;
		}
	};
	typedef Token Terminal, Nonterminal;
	typedef std::vector<Token> Tokens; // 记号流
	typedef Tokens Candidate; // 候选式
	typedef std::vector<Candidate> Candidates; // 候选式集

	/// <summary>
	/// 产生式
	/// </summary>
	struct Production
	{
		Nonterminal left; // 左部非终结符
		Candidate right; // 右部单个候选式

		Production() :
			left(), right()
		{
		}

		Production(const Token& nonterminal) :
			left(nonterminal), right()
		{
		}

		Production(const Token& nonterminal, const Candidate& candidate) :
			left(nonterminal), right(candidate)
		{
		}

		Production(const Production& _Right) :
			left(_Right.left), right(_Right.right)
		{
		}

		Production(Production&& _Right) :
			left(_Right.left), right(_Right.right)
		{
		}

		Production& operator=(const Production& _Right)
		{
			left = _Right.left;
			right = _Right.right;
			return *this;
		}

		Production& operator=(Production&& _Right)
		{
			left = _Right.left;
			right = _Right.right;
			return *this;
		}

		bool operator==(const Production& _Right) const
		{
			if (left != _Right.left)
				return false;
			if (right.size() == _Right.right.size())
			{
				for (size_t pos = 0; pos < right.size(); ++pos)
				{
					if (right[pos] != _Right.right[pos])
						return false;
				}
				return true;
			}
			return false;
		}

		bool operator!=(const Production& _Right) const
		{
			return !(*this == _Right);
		}

		bool operator<(const Production& _Right) const
		{
			if (left < _Right.left)
				return true;
			else if (left == _Right.left)
			{
				if (right.size() < _Right.right.size())
					return true;
				else if (right.size() == _Right.right.size())
				{
					for (size_t pos = 0; pos < right.size(); ++pos)
					{
						if (!(right[pos] < _Right.right[pos]))
							return false;
					}
					return true;
				}
			}
			return false;
		}
	};
	typedef std::set<Terminal> TerminalSet; // 终结符集
	typedef std::set<Nonterminal> NonterminalSet; // 非终结符集
	typedef std::set<Token> NoteSet; // 符号集
	typedef std::list<Production> ProductionSet; // 产生式集

	typedef std::set<Terminal> First; // 终结符集
	typedef std::map<Nonterminal, First> FirstSet; // FIRST集
	typedef std::set<Terminal> Follow; // 终结符集
	typedef std::map<Nonterminal, Follow> FollowSet; // FOLLOW集

	typedef std::map<Terminal, int> ColumnsIndex; // 列向量索引
	typedef std::map<Nonterminal, int> RowsIndex; // 行向量索引
	typedef std::map<std::pair<int, int>, Production> Sheet; // 分析表

	typedef std::map<std::pair<int, int>, std::vector<Production>> DuplicateSheet; // 多重项

public:
	LLGrammar()
	{
	}

	~LLGrammar()
	{
	}

public:
	NoteSet GetAllNotes() const;
	ProductionSet GetAllProductions() const;
	Nonterminal GetStartNonterminal() const;

	bool InsertTerminal(const char statement[]);
	bool InsertNonterminal(const char statement[]);
	bool InsertProduction(const char statement[]);
	bool InsertProduction(const Production& production);
	bool SetStartNonterminal(const char statement[]);

	LLGrammar ToLL() const;

	std::string GetAnalysisSheet();
	std::string Analyze(const char _Word[]);

private:
	bool isLL{ false };
	NoteSet notes;
	ProductionSet productions;
	Nonterminal start;

	RowsIndex rows;
	ColumnsIndex columns;
	Sheet sheet;
	DuplicateSheet dpsheet;

private:
	void EliminateLeftRecursion();
	void EliminateCommonLeftFactor();
	void Extend();

	FirstSet WorkOutFirstSet() const;
	FollowSet WorkOutFollowSet(FirstSet& firstSet) const;
};