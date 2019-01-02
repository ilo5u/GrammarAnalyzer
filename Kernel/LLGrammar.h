#pragma once

typedef std::string String, Statement;
typedef std::vector<Statement> Statements;

/// <summary>
/// �ķ���
/// </summary>
class LLGrammar
{
public:
	/// <summary>
	/// �Ǻ�
	/// </summary>
	struct Token
	{
		/* ���� */
		enum class Type
		{
			TERMINAL,    // �ս��
			NONTERMINAL, // ���ս��
			INVALID
		};
		Type type;
		std::string attribute; // ���ʽ

		Token() :
			type(Type::TERMINAL), attribute({ '@' })
		{
			// Ĭ�Ϲ���Ϊ�Ǻ�Epsilon
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

		/* ���¹�ϵ��������STL SET */
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
	typedef std::vector<Token> Tokens; // �Ǻ���
	typedef Tokens Candidate; // ��ѡʽ
	typedef std::vector<Candidate> Candidates; // ��ѡʽ��

	/// <summary>
	/// ����ʽ
	/// </summary>
	struct Production
	{
		Nonterminal left; // �󲿷��ս��
		Candidate right; // �Ҳ�������ѡʽ

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
	typedef std::set<Terminal> TerminalSet; // �ս����
	typedef std::set<Nonterminal> NonterminalSet; // ���ս����
	typedef std::set<Token> NoteSet; // ���ż�
	typedef std::list<Production> ProductionSet; // ����ʽ��

	typedef std::set<Terminal> First; // �ս����
	typedef std::map<Nonterminal, First> FirstSet; // FIRST��
	typedef std::set<Terminal> Follow; // �ս����
	typedef std::map<Nonterminal, Follow> FollowSet; // FOLLOW��

	typedef std::map<Terminal, int> ColumnsIndex; // ����������
	typedef std::map<Nonterminal, int> RowsIndex; // ����������
	typedef std::map<std::pair<int, int>, Production> Sheet; // ������

	typedef std::map<std::pair<int, int>, std::vector<Production>> DuplicateSheet; // ������

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